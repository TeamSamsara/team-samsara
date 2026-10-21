// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/Repositories/FirestoreInboxNotificationStore.cs
// Version : 1.2.0
// Latest commit: task/notifications-api
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
    public async Task<InboxNotification?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = await Notifications
                .Document(id)
                .GetSnapshotAsync(cancellationToken);

            return snapshot.Exists ? snapshot.ConvertTo<InboxNotification>() : null;
        }
        catch (RpcException exception)
        {
            throw new NotificationStoreException(
                $"Could not read notification '{id}' ({exception.StatusCode}).", exception);
        }
    }

    // Retrieves up to limit of the recipient's notifications, newest first and ties by id descending, starting just after the cursor when one is given; a cancelled caller gets the cancellation, any other server failure becomes NotificationStoreException
    public async Task<IReadOnlyList<InboxNotification>> ListByRecipientAsync(
        string recipientId,
        int limit,
        InboxCursor? after,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = Notifications
                .WhereEqualTo(nameof(InboxNotification.RecipientId), recipientId)
                .OrderByDescending(nameof(InboxNotification.CreatedAt));

            if (after is not null)
            {
                query = query.StartAfter(Timestamp.FromDateTimeOffset(after.CreatedAt), Notifications.Document(after.Id));
            }

            var snapshot = await query
                .Limit(limit)
                .GetSnapshotAsync(cancellationToken);

            return snapshot.Documents
                .Select(document => document.ConvertTo<InboxNotification>())
                .ToList();
        }
        catch (RpcException exception)
        {
            cancellationToken.ThrowIfCancellationRequested();

            throw new NotificationStoreException(
                $"Could not list the notifications of member '{recipientId}' ({exception.StatusCode}).", exception);
        }
    }

    // Counts the recipient's notifications that have no ReadAt; a cancelled caller gets the cancellation, any other server failure becomes NotificationStoreException
    public async Task<int> CountUnreadByRecipientAsync(string recipientId, CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = await Notifications
                .WhereEqualTo(nameof(InboxNotification.RecipientId), recipientId)
                .WhereEqualTo(nameof(InboxNotification.ReadAt), null)
                .Count()
                .GetSnapshotAsync(cancellationToken);

            return (int)(snapshot.Count ?? 0);
        }
        catch (RpcException exception)
        {
            cancellationToken.ThrowIfCancellationRequested();

            throw new NotificationStoreException(
                $"Could not count the unread notifications of member '{recipientId}' ({exception.StatusCode}).", exception);
        }
    }

    // Sets ReadAt in one transaction, so two calls cannot both think they were first; false when the notification is missing or belongs to someone else
    public async Task<bool> MarkReadAsync(
        string recipientId,
        string id,
        DateTimeOffset readAt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _firestoreDb.RunTransactionAsync(
                async transaction =>
                {
                    var reference = Notifications.Document(id);
                    var snapshot = await transaction.GetSnapshotAsync(reference, cancellationToken);

                    if (!snapshot.Exists)
                    {
                        return false;
                    }

                    var notification = snapshot.ConvertTo<InboxNotification>();

                    if (notification.RecipientId != recipientId)
                    {
                        return false;
                    }

                    if (notification.ReadAt is null)
                    {
                        transaction.Update(reference, BuildReadUpdate(readAt));
                    }

                    return true;
                },
                cancellationToken: cancellationToken);
        }
        catch (RpcException exception)
        {
            cancellationToken.ThrowIfCancellationRequested();

            throw new NotificationStoreException(
                $"Could not mark notification '{id}' as read ({exception.StatusCode}).", exception);
        }
    }

    // Sets ReadAt on up to one batch of the recipient's unread notifications and returns how many it found, 0 once none are left; a cancelled caller gets the cancellation, any other server failure becomes NotificationStoreException
    public async Task<int> MarkBatchReadAsync(
        string recipientId,
        DateTimeOffset readAt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = await Notifications
                .WhereEqualTo(nameof(InboxNotification.RecipientId), recipientId)
                .WhereEqualTo(nameof(InboxNotification.ReadAt), null)
                .Limit(WriteBatchSize)
                .GetSnapshotAsync(cancellationToken);

            if (snapshot.Count == 0)
            {
                return 0;
            }

            var batch = _firestoreDb.StartBatch();
            var update = BuildReadUpdate(readAt);

            foreach (var document in snapshot.Documents)
            {
                batch.Update(document.Reference, update);
            }

            await batch.CommitAsync(cancellationToken);

            return snapshot.Count;
        }
        catch (RpcException exception)
        {
            cancellationToken.ThrowIfCancellationRequested();

            throw new NotificationStoreException(
                $"Could not mark the notifications of member '{recipientId}' as read ({exception.StatusCode}).", exception);
        }
    }

    // Deletes up to one batch of the recipient's notifications and returns how many it found, 0 once none are left; a cancelled caller gets the cancellation, any other server failure becomes NotificationStoreException
    public async Task<int> DeleteBatchByRecipientAsync(string recipientId, CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = await Notifications
                .WhereEqualTo(nameof(InboxNotification.RecipientId), recipientId)
                .Limit(WriteBatchSize)
                .GetSnapshotAsync(cancellationToken);

            if (snapshot.Count == 0)
            {
                return 0;
            }

            var batch = _firestoreDb.StartBatch();

            foreach (var document in snapshot.Documents)
            {
                batch.Delete(document.Reference);
            }

            await batch.CommitAsync(cancellationToken);

            return snapshot.Count;
        }
        catch (RpcException exception)
        {
            cancellationToken.ThrowIfCancellationRequested();

            throw new NotificationStoreException(
                $"Could not delete the notifications of member '{recipientId}' ({exception.StatusCode}).", exception);
        }
    }

    #endregion

    #region Private Methods

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
