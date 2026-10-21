// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/Models/NotificationPage.cs
// Version : 1.0.0
// Latest commit: task/notifications-api
// Author : Gerrah
// Purpose : One page of a member's inbox, newest first, with the cursor for the next page or null when there is none.

namespace TeamSamsara.Modules.Notification.Models;

public record NotificationPage(IReadOnlyList<NotificationSummary> Items, string? NextCursor);
