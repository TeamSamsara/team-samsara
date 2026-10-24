// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/Models/NotificationSettings.cs
// Version : 1.1.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Configuration for how long notifications are kept, how many a member keeps and how long a storage write may take.

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

    // How many notifications one member keeps; past this the oldest read ones are removed first, and an unanswered sign-in notice is never removed
    [Range(10, 500)]
    public int MaxPerMember { get; set; } = 50;

    // How long one storage call may take before the notification is reported as failed
    [Range(1, 30)]
    public int WriteTimeoutSeconds { get; set; } = 5;

    #endregion
}
