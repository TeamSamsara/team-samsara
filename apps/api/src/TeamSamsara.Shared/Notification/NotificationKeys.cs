// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Notification/NotificationKeys.cs
// Version : 1.0.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Notification keys that more than one module needs to know, so the module that
// records a notification and the module that stores the inbox agree on the same text.

namespace TeamSamsara.Shared.Notification;

public static class NotificationKeys
{
    #region Fields

    public const string NewLogin = "security.new-login";
    #endregion
}
