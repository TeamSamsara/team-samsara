// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/Repositories/IInboxNotificationStore.cs
// Version : 1.2.0
// Latest commit: task/notifications-api
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

    // Retrieves up to limit of the recipient's notifications, newest first (ties by id, descending), starting just after the cursor when one is given; includes expired ones the TTL has not deleted yet, and throws NotificationStoreException when the read fails
    public Task<IReadOnlyList<InboxNotification>> ListByRecipientAsync(string recipientId, int limit, InboxCursor? after, CancellationToken cancellationToken = default);

    // Counts the recipient's notifications that have no ReadAt; throws NotificationStoreException when the count fails
    public Task<int> CountUnreadByRecipientAsync(string recipientId, CancellationToken cancellationToken = default);

    // Sets ReadAt on one of the recipient's notifications unless it already has one; false when no such notification exists or it belongs to someone else, and throws NotificationStoreException when the update fails
    public Task<bool> MarkReadAsync(string recipientId, string id, DateTimeOffset readAt, CancellationToken cancellationToken = default);

    // Sets ReadAt on up to one batch of the recipient's unread notifications and returns how many it found, 0 once none are left; throws NotificationStoreException when the update fails
    public Task<int> MarkBatchReadAsync(string recipientId, DateTimeOffset readAt, CancellationToken cancellationToken = default);

    // Deletes one batch of the recipient's notifications and returns how many it deleted, 0 once none are left; throws NotificationStoreException when the deletion fails
    public Task<int> DeleteBatchByRecipientAsync(string recipientId, CancellationToken cancellationToken = default);

    #endregion
}
