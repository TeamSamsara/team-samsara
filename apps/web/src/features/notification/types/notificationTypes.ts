// File : /team-samsara/apps/web/src/features/notifications/types/notificationTypes.ts
// Version : 1.0.0
// Latest commit: task/web-notifications
// Author : Gerrah
// Purpose : The notification inbox API's response shapes. Keys and data are
// open strings, so an unknown notification still parses and can be shown.

export interface NotificationItem {
  readonly id: string;
  readonly key: string;
  readonly data: Readonly<Record<string, string>>;
  readonly createdAt: string;
  readonly readAt: string | null;
}

export interface NotificationPage {
  readonly items: readonly NotificationItem[];
  readonly nextCursor: string | null;
}
