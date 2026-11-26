// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/Models/NotificationSummary.cs
// Version : 1.0.0
// Latest commit: task/notifications-api
// Author : Gerrah
// Purpose : One notification as the inbox endpoints show it to its owner; the frontend derives the category from Key.

namespace TeamSamsara.Modules.Notification.Models;

public record NotificationSummary(
    string Id,
    string Key,
    IReadOnlyDictionary<string, string> Data,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt
);
