// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/Repositories/IInboxNotificationStore.cs
// Version : 1.0.0
// Latest commit: feat/notification-send-path
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

    #endregion
}
