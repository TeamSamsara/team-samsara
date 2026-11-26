// File : /team-samsara/apps/web/src/features/notification/hooks/useSignInAlert.ts
// Version : 1.0.0
// Latest commit: task/web-notifications
// Author : Gerrah
// Purpose : Finds the newest unread new-sign-in notice for the banner. The
// list is only fetched while the unread count is above zero, and it shares its
// cache with the panel, so opening the panel afterwards costs no extra call.

import { NotificationKeys } from "../constants/notificationKeys";
import type { NotificationItem } from "../types/notificationTypes";
import { useNotificationList } from "./useNotificationList";
import { useUnreadCount } from "./useUnreadCount";

function isUnreadSignIn(item: NotificationItem): boolean {
  return item.key === NotificationKeys.newLogin && item.readAt === null;
}

export function useSignInAlert(): NotificationItem | null {
  const { data: unreadCount } = useUnreadCount();
  const hasUnread = (unreadCount ?? 0) > 0;
  const list = useNotificationList(hasUnread);

  if (!hasUnread || list.data === undefined) {
    return null;
  }

  for (const page of list.data.pages) {
    const alert = page.items.find(isUnreadSignIn);

    if (alert !== undefined) {
      return alert;
    }
  }

  return null;
}
