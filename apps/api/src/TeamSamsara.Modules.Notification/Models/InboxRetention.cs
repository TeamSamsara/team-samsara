// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/Models/InboxRetention.cs
// Version : 1.0.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : The rules for what a member may clear from their inbox and which notifications go
// first when the inbox is over its limit. Pure rules, so they are easy to test on their own.

using TeamSamsara.Shared.Notification;

namespace TeamSamsara.Modules.Notification.Models;

public static class InboxRetention
{
    #region Public Methods

    // A sign-in notice the member has not answered yet. The site keeps showing it as an alert
    // until they do, so it must not be deleted or trimmed away.
    public static bool IsUnansweredSignIn(InboxNotification notification)
    {
        return notification.Key == NotificationKeys.NewLogin && notification.ReadAt is null;
    }

    // Everything can be cleared except an unanswered sign-in notice
    public static bool IsClearable(InboxNotification notification)
    {
        return !IsUnansweredSignIn(notification);
    }

    // The notifications to remove so the inbox fits the limit, given the member's notifications
    // newest first. The oldest read ones go first, then the oldest unread ones; an unanswered
    // sign-in notice is never chosen, so the inbox may stay over the limit because of them.
    public static IReadOnlyList<InboxNotification> SelectForTrim(
        IReadOnlyList<InboxNotification> newestFirst,
        int limit)
    {
        var excess = newestFirst.Count - limit;

        if (excess <= 0)
        {
            return [];
        }

        var oldestFirst = newestFirst.Reverse().ToList();
        var read = oldestFirst.Where(IsRead);
        var unread = oldestFirst.Where(notification => !IsRead(notification) && IsClearable(notification));

        return read.Concat(unread).Take(excess).ToList();
    }

    #endregion

    #region Private Methods

    private static bool IsRead(InboxNotification notification)
    {
        return notification.ReadAt is not null;
    }

    #endregion
}
