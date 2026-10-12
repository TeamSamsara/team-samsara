// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/Repositories/NotificationFirestoreCollections.cs
// Version : 1.0.0
// Latest commit: feat/notification-send-path
// Author : Gerrah
// Purpose : Firestore collection names owned by the Notification module.

namespace TeamSamsara.Modules.Notification.Repositories;

public static class NotificationFirestoreCollections
{
    #region Fields

    // Inbox notifications, one document each, keyed by a generated or dedupe-derived id; deleted by the TTL policy on ExpiresAt
    public const string Notifications = "notifications";

    #endregion
}
