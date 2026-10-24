// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/Repositories/FirestoreInboxNotificationStore.cs
// Version : 1.3.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Firestore implementation of the inbox notification store.

using Google.Cloud.Firestore;
using Grpc.Core;
using TeamSamsara.Modules.Notification.Models;

namespace TeamSamsara.Modules.Notification.Repositories;

public class FirestoreInboxNotificationStore : IInboxNotificationStore
{
    #region Fields

    // The most writes one Firestore batch accepts
    private const int WriteBatchSize = 500;

    private readonly FirestoreDb _firestoreDb;

    #endregion

    #region Properties

    private CollectionReference Notifications => _firestoreDb.Collection(NotificationFirestoreCollections.Notifications);

    #endregion

    #region Constructors

    public FirestoreInboxNotificationStore(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }

    #endregion

    #region Public Methods

    // Creates the document; false if one already has the id, NotificationStoreException on any other server failure
    public async Task<bool> TryCreateAsync(InboxNotification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await Notifications
                .Document(notification.Id)
                .CreateAsync(notification, cancellationToken);

            return true;
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.AlreadyExists)
        {
            return false;
        }
        catch (RpcException exception)
        {
            throw new NotificationStoreException(
                $"Could not write notification '{notification.Id}' ({exception.StatusCode}).", exception);
        }
    }

    // Retrieves a notification by id, or null if none exists
    public Task<InboxNotification?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return RunAsync(
            async () =>
            {
                var snapshot = await Notifications.Document(id).GetSnapshotAsync(cancellationToken);

                return snapshot.Exists ? snapshot.ConvertTo<InboxNotification>() : null;
            },
            $"Could not read notification '{id}'",
            cancellationToken);
    }

    // Retrieves up to limit of the recipient's notifications, newest first and ties by id descending, starting just after the cursor when one is given; a cancelled caller gets the cancellation, any other server failure becomes NotificationStoreException
    public Task<IReadOnlyList<InboxNotification>> ListByRecipientAsync(
        string recipientId,
        int limit,
        InboxCursor? after,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(
            () => ReadPageAsync(recipientId, limit, after, cancellationToken),
            $"Could not list the notifications of member '{recipientId}'",
            cancellationToken);
    }

    // Counts the recipient's notifications that have no ReadAt; a cancelled caller gets the cancellation, any other server failure becomes NotificationStoreException
    public Task<int> CountUnreadByRecipientAsync(string recipientId, CancellationToken cancellationToken = default)
    {
        return RunAsync(
            async () =>
            {
                var snapshot = await Unread(recipientId).Count().GetSnapshotAsync(cancellationToken);

                return (int)(snapshot.Count ?? 0);
            },
            $"Could not count the unread notifications of member '{recipientId}'",
            cancellationToken);
    }

    // Sets ReadAt in one transaction, so two calls cannot both think they were first; false when the notification is missing or belongs to someone else
    public Task<bool> MarkReadAsync(
        string recipientId,
        string id,
        DateTimeOffset readAt,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(
            () => InTransactionAsync(
                transaction => MarkReadInAsync(transaction, recipientId, id, readAt, cancellationToken),
                cancellationToken),
            $"Could not mark notification '{id}' as read",
            cancellationToken);
    }

    // Sets ReadAt on up to one batch of the recipient's unread notifications and returns how many it found, 0 once none are left; a cancelled caller gets the cancellation, any other server failure becomes NotificationStoreException
    public Task<int> MarkBatchReadAsync(
        string recipientId,
        DateTimeOffset readAt,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(
            async () =>
            {
                var snapshot = await Unread(recipientId).Limit(WriteBatchSize).GetSnapshotAsync(cancellationToken);

                await UpdateDocumentsAsync(References(snapshot.Documents), BuildReadUpdate(readAt), cancellationToken);

                return snapshot.Count;
            },
            $"Could not mark the notifications of member '{recipientId}' as read",
            cancellationToken);
    }

    // Reads the notification and deletes it in one transaction, so what canDelete judged is what gets deleted; NotFound when it is missing or belongs to someone else, NotClearable when canDelete refuses it
    public Task<NotificationDeleteStatus> DeleteAsync(
        string recipientId,
        string id,
        Func<InboxNotification, bool> canDelete,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(
            () => InTransactionAsync(
                transaction => DeleteInAsync(transaction, recipientId, id, canDelete, cancellationToken),
                cancellationToken),
            $"Could not delete notification '{id}'",
            cancellationToken);
    }

    // Looks at the recipient's newest batch of notifications and deletes the read ones among them, so it needs no index beyond the one the list uses; a cancelled caller gets the cancellation, any other server failure becomes NotificationStoreException
    public Task<int> DeleteReadBatchAsync(string recipientId, CancellationToken cancellationToken = default)
    {
        return RunAsync(
            async () =>
            {
                var snapshot = await NewestFirst(recipientId).Limit(WriteBatchSize).GetSnapshotAsync(cancellationToken);
                var read = References(snapshot.Documents.Where(IsRead)).ToList();

                await DeleteDocumentsAsync(read, cancellationToken);

                return read.Count;
            },
            $"Could not delete the read notifications of member '{recipientId}'",
            cancellationToken);
    }

    // Deletes the notifications one Firestore batch at a time; a cancelled caller gets the cancellation, any other server failure becomes NotificationStoreException
    public Task DeleteManyAsync(
        IReadOnlyList<InboxNotification> notifications,
        CancellationToken cancellationToken = default)
    {
        return RunVoidAsync(
            () => DeleteDocumentsAsync(
                notifications.Select(notification => Notifications.Document(notification.Id)),
                cancellationToken),
            $"Could not delete {notifications.Count} notifications",
            cancellationToken);
    }

    // Deletes up to one batch of the recipient's notifications and returns how many it found, 0 once none are left; a cancelled caller gets the cancellation, any other server failure becomes NotificationStoreException
    public Task<int> DeleteBatchByRecipientAsync(string recipientId, CancellationToken cancellationToken = default)
    {
        return RunAsync(
            async () =>
            {
                var snapshot = await ByRecipient(recipientId).Limit(WriteBatchSize).GetSnapshotAsync(cancellationToken);

                await DeleteDocumentsAsync(References(snapshot.Documents), cancellationToken);

                return snapshot.Count;
            },
            $"Could not delete the notifications of member '{recipientId}'",
            cancellationToken);
    }

    #endregion

    #region Private Methods

    // Runs the storage work; a cancelled caller gets the cancellation, any other server failure becomes NotificationStoreException
    private static async Task<T> RunAsync<T>(Func<Task<T>> work, string failure, CancellationToken cancellationToken)
    {
        try
        {
            return await work();
        }
        catch (RpcException exception)
        {
            cancellationToken.ThrowIfCancellationRequested();

            throw new NotificationStoreException($"{failure} ({exception.StatusCode}).", exception);
        }
    }

    private static async Task RunVoidAsync(Func<Task> work, string failure, CancellationToken cancellationToken)
    {
        await RunAsync(
            async () =>
            {
                await work();

                return true;
            },
            failure,
            cancellationToken);
    }

    private Query ByRecipient(string recipientId)
    {
        return Notifications.WhereEqualTo(nameof(InboxNotification.RecipientId), recipientId);
    }

    private Query Unread(string recipientId)
    {
        return ByRecipient(recipientId).WhereEqualTo(nameof(InboxNotification.ReadAt), null);
    }

    private Query NewestFirst(string recipientId)
    {
        return ByRecipient(recipientId).OrderByDescending(nameof(InboxNotification.CreatedAt));
    }

    // One page of the recipient's notifications, starting just after the cursor when one is given
    private async Task<IReadOnlyList<InboxNotification>> ReadPageAsync(
        string recipientId,
        int limit,
        InboxCursor? after,
        CancellationToken cancellationToken)
    {
        var query = NewestFirst(recipientId);

        if (after is not null)
        {
            query = query.StartAfter(Timestamp.FromDateTimeOffset(after.CreatedAt), Notifications.Document(after.Id));
        }

        var snapshot = await query.Limit(limit).GetSnapshotAsync(cancellationToken);

        return snapshot.Documents
            .Select(document => document.ConvertTo<InboxNotification>())
            .ToList();
    }

    private Task<T> InTransactionAsync<T>(Func<Transaction, Task<T>> work, CancellationToken cancellationToken)
    {
        return _firestoreDb.RunTransactionAsync(work, cancellationToken: cancellationToken);
    }

    // The notification when it exists and belongs to the recipient, otherwise null
    private async Task<InboxNotification?> ReadOwnedAsync(
        Transaction transaction,
        string recipientId,
        string id,
        CancellationToken cancellationToken)
    {
        var snapshot = await transaction.GetSnapshotAsync(Notifications.Document(id), cancellationToken);

        if (!snapshot.Exists)
        {
            return null;
        }

        var notification = snapshot.ConvertTo<InboxNotification>();

        return notification.RecipientId == recipientId ? notification : null;
    }

    private async Task<bool> MarkReadInAsync(
        Transaction transaction,
        string recipientId,
        string id,
        DateTimeOffset readAt,
        CancellationToken cancellationToken)
    {
        var notification = await ReadOwnedAsync(transaction, recipientId, id, cancellationToken);

        if (notification is null)
        {
            return false;
        }

        if (notification.ReadAt is null)
        {
            transaction.Update(Notifications.Document(id), BuildReadUpdate(readAt));
        }

        return true;
    }

    private async Task<NotificationDeleteStatus> DeleteInAsync(
        Transaction transaction,
        string recipientId,
        string id,
        Func<InboxNotification, bool> canDelete,
        CancellationToken cancellationToken)
    {
        var notification = await ReadOwnedAsync(transaction, recipientId, id, cancellationToken);

        if (notification is null)
        {
            return NotificationDeleteStatus.NotFound;
        }

        if (!canDelete(notification))
        {
            return NotificationDeleteStatus.NotClearable;
        }

        transaction.Delete(Notifications.Document(id));

        return NotificationDeleteStatus.Deleted;
    }

    private Task DeleteDocumentsAsync(IEnumerable<DocumentReference> references, CancellationToken cancellationToken)
    {
        return CommitInBatchesAsync(references, (batch, reference) => batch.Delete(reference), cancellationToken);
    }

    private Task UpdateDocumentsAsync(
        IEnumerable<DocumentReference> references,
        Dictionary<string, object> update,
        CancellationToken cancellationToken)
    {
        return CommitInBatchesAsync(references, (batch, reference) => batch.Update(reference, update), cancellationToken);
    }

    // Applies one write to every document, one Firestore batch at a time
    private async Task CommitInBatchesAsync(
        IEnumerable<DocumentReference> references,
        Action<WriteBatch, DocumentReference> write,
        CancellationToken cancellationToken)
    {
        foreach (var chunk in references.Chunk(WriteBatchSize))
        {
            var batch = _firestoreDb.StartBatch();

            foreach (var reference in chunk)
            {
                write(batch, reference);
            }

            await batch.CommitAsync(cancellationToken);
        }
    }

    private static IEnumerable<DocumentReference> References(IEnumerable<DocumentSnapshot> documents)
    {
        return documents.Select(document => document.Reference);
    }

    private static bool IsRead(DocumentSnapshot document)
    {
        return document.ConvertTo<InboxNotification>().ReadAt is not null;
    }

    // The one field change that marks a notification as read
    private static Dictionary<string, object> BuildReadUpdate(DateTimeOffset readAt)
    {
        return new Dictionary<string, object>
        {
            [nameof(InboxNotification.ReadAt)] = Timestamp.FromDateTimeOffset(readAt)
        };
    }

    #endregion
}
