// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/Models/NotificationSettings.cs
// Version : 1.0.0
// Latest commit: feat/notification-send-path
// Author : Gerrah
// Purpose : Configuration for how long notifications are kept and how long a storage write may take.

using System.ComponentModel.DataAnnotations;

namespace TeamSamsara.Modules.Notification.Models;

public class NotificationSettings
{
    #region Fields

    public const string SectionName = "Notification";

    #endregion

    #region Properties

    // How long a notification stays in the inbox before the Firestore TTL policy may delete it; a policy choice, not a confirmed product requirement
    [Range(1, 365)]
    public int RetentionDays { get; set; } = 90;

    // How long one storage call may take before the notification is reported as failed
    [Range(1, 30)]
    public int WriteTimeoutSeconds { get; set; } = 5;

    #endregion
}
