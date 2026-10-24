// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/Endpoints/NotificationRoutes.cs
// Version : 1.1.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Route paths of the Notification module's endpoints, shared between the endpoint class and its tests.

namespace TeamSamsara.Modules.Notification.Endpoints;

public static class NotificationRoutes
{
    #region Fields

    public const string Group = "/notifications";

    public const string Root = "/";
    public const string UnreadCount = "/unread-count";
    public const string Read = "/{id}/read";
    public const string ReadAll = "/read-all";

    // DELETE: one notification
    public const string Item = "/{id}";

    // DELETE: every read notification; a literal path wins over "/{id}", and "read" is never a valid id anyway
    public const string ClearRead = "/read";

    #endregion
}
