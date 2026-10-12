// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/Repositories/NotificationStoreException.cs
// Version : 1.0.0
// Latest commit: feat/notification-send-path
// Author : Gerrah
// Purpose : Signals that the notification store could not complete a call, so handlers never see the database's own exception types.

namespace TeamSamsara.Modules.Notification.Repositories;

public class NotificationStoreException : Exception
{
    public NotificationStoreException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
