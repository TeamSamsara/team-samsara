// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Notification/NotificationStatus.cs
// Version : 1.0.0
// Latest commit: feat/notification-send-path
// Author : Gerrah
// Purpose : The outcome of asking for a notification to be recorded.

namespace TeamSamsara.Shared.Notification;

public enum NotificationStatus
{
    // The notification was written to the member's inbox
    Stored,

    // The same dedupe key and content were already stored, so nothing was written
    Duplicate,

    // The dedupe key already belongs to a notification with different content, so nothing was written
    Conflict,

    // The notification could not be confirmed as stored and may be lost
    Failed
}
