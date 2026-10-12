// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/Repositories/IInboxNotificationStore.cs
// Version : 1.1.0
// Latest commit: feat/notification-erasure
// Author : Gerrah
// Purpose : Storage contract for inbox notifications.

using TeamSamsara.Modules.Notification.Models;

namespace TeamSamsara.Modules.Notification.Repositories;

public interface IInboxNotificationStore
{
    #region Public Methods

    // Writes the notification unless a document already has its id; false when one does, and throws NotificationStoreException when the write cannot be confirmed
    public Task<bool> TryCreateAsync(InboxNotification notification, CancellationToken cancellationToken = default);

    // Retrieves a notification by id, or null if none exists
    public Task<InboxNotification?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    // Deletes one batch of the recipient's notifications and returns how many it deleted, 0 once none are left; throws NotificationStoreException when the deletion fails
    public Task<int> DeleteBatchByRecipientAsync(string recipientId, CancellationToken cancellationToken = default);

    #endregion
}
