// File : /team-samsara/apps/web/src/features/notifications/constants/notificationApiRoutes.ts
// Version : 1.0.0
// Latest commit: task/web-notifications
// Author : Gerrah
// Purpose : Backend API routes for the member's notification inbox.

export const NotificationApiRoutes = {
  list: "/notifications",
  unreadCount: "/notifications/unread-count",
  readAll: "/notifications/read-all",
  read: (id: string) => `/notifications/${id}/read`,
} as const;
