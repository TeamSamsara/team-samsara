// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Notification/NotificationResult.cs
// Version : 1.0.0
// Latest commit: feat/notification-send-path
// Author : Gerrah
// Purpose : What a notification request produced: its outcome and, when one exists, the id of the stored notification.

namespace TeamSamsara.Shared.Notification;

public record NotificationResult(NotificationStatus Status, string? Id);
