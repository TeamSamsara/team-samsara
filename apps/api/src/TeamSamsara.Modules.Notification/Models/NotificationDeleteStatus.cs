// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/Models/NotificationDeleteStatus.cs
// Version : 1.0.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : The outcome of a member deleting one of their notifications. The endpoint turns
// these into responses.

namespace TeamSamsara.Modules.Notification.Models;

public enum NotificationDeleteStatus
{
    // The notification was deleted
    Deleted,

    // No such notification, or it belongs to someone else. Deliberately not told apart, so
    // ids cannot be probed.
    NotFound,

    // An unanswered sign-in notice: the member has to answer it before it can go
    NotClearable
}
