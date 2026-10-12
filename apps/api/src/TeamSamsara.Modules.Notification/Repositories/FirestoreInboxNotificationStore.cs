// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/Repositories/FirestoreInboxNotificationStore.cs
// Version : 1.0.0
// Latest commit: feat/notification-send-path
// Author : Gerrah
// Purpose : Firestore implementation of the inbox notification store.

using Google.Cloud.Firestore;
using Grpc.Core;
using TeamSamsara.Modules.Notification.Models;

namespace TeamSamsara.Modules.Notification.Repositories;

public class FirestoreInboxNotificationStore : IInboxNotificationStore
{
    #region Fields

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

    #endregion
}
