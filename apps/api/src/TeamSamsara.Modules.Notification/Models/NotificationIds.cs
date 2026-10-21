// File : /team-samsara/apps/api/TeamSamsara.Modules.Notification/Models/NotificationIds.cs
// Version : 1.0.0
// Latest Commit : task/notification-api
// Author : Gerrah
// Purpose : Tells whether text has the shape of a notification id, so a malformed id never reaches the storage layer.

using System.Text.RegularExpressions;

namespace TeamSamsara.Modules.Notification.Models;

public static class NotificationIds
{
    #region Fields
    // 32 lowercase hex characters, or 64 when the ids was built from dedupe key
    private static readonly Regex _idPattern = new(@"^([0-9a-f]{32}|[0-9a-f]{64})\z", RegexOptions.CultureInvariant);
    #endregion

    #region Public Methods
    // True when the value looks like a notification id: says nothing about whether one exists
    public static bool IsValid(string? value)
    {
        return value is not null && _idPattern.IsMatch(value);
    }
    #endregion
}
