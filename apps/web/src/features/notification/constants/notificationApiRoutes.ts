// File : /team-samsara/apps/web/src/features/notification/constants/notificationApiRoutes.ts
// Version : 1.1.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Backend API routes for the member's notification inbox.

export const NotificationApiRoutes = {
  list: "/notifications",
  unreadCount: "/notifications/unread-count",
  readAll: "/notifications/read-all",
  read: (id: string) => `/notifications/${id}/read`,
  remove: (id: string) => `/notifications/${id}`,
  clearRead: "/notifications/read",
} as const;
