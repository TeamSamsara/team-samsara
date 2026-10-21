// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/Handlers/IInboxService.cs
// Version : 1.0.0
// Latest commit: task/notifications-api
// Author : Gerrah
// Purpose : What a member can do with their own inbox: read a page of it, count what is unread, mark one notification or all of them as read.

using TeamSamsara.Modules.Notification.Models;

namespace TeamSamsara.Modules.Notification.Handlers;

public interface IInboxService
{
    #region Public Methods

    // Returns one page of the member's notifications, newest first, without the ones that have expired; null when the cursor is malformed, and throws NotificationStoreException when storage fails
    public Task<NotificationPage?> ListAsync(string recipientId, int? limit, string? cursor, CancellationToken cancellationToken = default);

    // Counts the member's unread notifications, and throws NotificationStoreException when storage fails
    public Task<int> CountUnreadAsync(string recipientId, CancellationToken cancellationToken = default);

    // Marks one of the member's notifications as read, keeping the time of the first call when it was already read; false when the id is malformed, unknown or someone else's, and throws NotificationStoreException when storage fails
    public Task<bool> MarkReadAsync(string recipientId, string id, CancellationToken cancellationToken = default);

    // Marks every unread notification of the member as read and returns how many it marked, and throws NotificationStoreException when storage fails
    public Task<int> MarkAllReadAsync(string recipientId, CancellationToken cancellationToken = default);

    #endregion
}
