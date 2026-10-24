// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/Models/UnreadCountResponse.cs
// Version : 1.0.0
// Latest commit: task/notifications-api
// Author : Gerrah
// Purpose : The answer to "how many unread notifications do I have": the number the frontend shows on the bell.

namespace TeamSamsara.Modules.Notification.Models;

public record UnreadCountResponse(int UnreadCount);
