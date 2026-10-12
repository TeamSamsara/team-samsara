// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Notification/INotificationSender.cs
// Version : 1.0.0
// Latest commit: feat/notification-send-path
// Author : Gerrah
// Purpose : Contract any module uses to put a notification in a member's inbox.

namespace TeamSamsara.Shared.Notification;

public interface INotificationSender
{
    #region Public Methods

    // Records the notification; throws ArgumentException for a malformed request and OperationCanceledException when cancelled, and reports storage failures as NotificationStatus.Failed instead of throwing
    public Task<NotificationResult> SendNotificationAsync(NotificationRequest request, CancellationToken cancellationToken = default);

    #endregion
}
