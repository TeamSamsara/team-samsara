// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Notification/NotificationRequest.cs
// Version : 1.0.0
// Latest commit: feat/notification-send-path
// Author : Gerrah
// Purpose : A request to record, in one member's inbox, that something happened that concerns them.

namespace TeamSamsara.Shared.Notification;

public record NotificationRequest(
    string RecipientId,
    string Key,
    IReadOnlyDictionary<string, string>? Data = null,
    string? DedupeKey = null);
