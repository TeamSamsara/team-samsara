// File : /team-samsara/apps/web/src/features/notification/constants/notificationKeys.ts
// Version : 1.1.0
// Latest commit: task/web-notifications
// Author : Gerrah
// Purpose : Known notification keys, page size, cache timings and query key
// parts shared by the notification hooks.

export const NotificationKeys = {
  newLogin: "security.new-login",
} as const;

export const NotificationPaging = {
  pageSize: 20,
} as const;

export const NotificationCache = {
  unreadCountStaleMs: 30_000,
  unreadCountPollMs: 15_000,
  listStaleMs: 15_000,
} as const;

export const NotificationQueryScope = {
  root: "notifications",
  unreadCount: "unread-count",
  list: "list",
} as const;
