// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/Repositories/FirestoreInboxNotificationStore.cs
// Version : 1.1.0
// Latest commit: feat/notification-erasure
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
    private const int DeleteBatchSize = 500;

    private readonly FirestoreDb _firestoreDb;

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
            await _firestoreDb
                .Collection(NotificationFirestoreCollections.Notifications)
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
            var snapshot = await _firestoreDb
                .Collection(NotificationFirestoreCollections.Notifications)
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

    // Deletes up to one batch of the recipient's notifications and returns how many it found, 0 once none are left; a cancelled caller gets the cancellation, any other server failure becomes NotificationStoreException
    public async Task<int> DeleteBatchByRecipientAsync(string recipientId, CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = await _firestoreDb
                .Collection(NotificationFirestoreCollections.Notifications)
                .WhereEqualTo(nameof(InboxNotification.RecipientId), recipientId)
                .Limit(DeleteBatchSize)
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
}
