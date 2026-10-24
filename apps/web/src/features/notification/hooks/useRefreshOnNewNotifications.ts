// File : /team-samsara/apps/web/src/features/notification/hooks/useRefreshOnNewNotifications.ts
// Version : 1.0.0
// Latest commit: task/web-notifications
// Author : Gerrah
// Purpose : Refreshes the notification list when the unread count changes by
// itself, so an open panel or the sign-in banner shows a new notice without a
// page reload. It renders nothing and is used once, by the bell.

import { useEffect, useRef } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useAuth } from "@/app/useAuth";
import { notificationListQueryKey } from "./useNotificationList";
import { useUnreadCount } from "./useUnreadCount";

export function useRefreshOnNewNotifications(): void {
  const queryClient = useQueryClient();
  const { userId } = useAuth();
  const { data: unreadCount } = useUnreadCount();
  const lastCount = useRef<number | undefined>(undefined);

  useEffect(() => {
    if (unreadCount === undefined) {
      return;
    }

    const hasChanged =
      lastCount.current !== undefined && lastCount.current !== unreadCount;

    lastCount.current = unreadCount;

    if (hasChanged) {
      void queryClient.invalidateQueries({
        queryKey: notificationListQueryKey(userId),
      });
    }
  }, [unreadCount, userId, queryClient]);
}
