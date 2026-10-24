// File : /team-samsara/apps/web/src/features/notifications/hooks/useNotificationList.ts
// Version : 1.0.0
// Latest commit: task/web-notifications
// Author : Gerrah
// Purpose : Reads the member's notifications page by page, following the
// API's cursor. It only runs while the panel is open, so a closed bell never
// loads the list. The cache is keyed by user id.

import { useInfiniteQuery } from "@tanstack/react-query";
import { useAuth } from "@/app/useAuth";
import {
  NotificationCache,
  NotificationQueryScope,
} from "../constants/notificationKeys";
import { getNotifications } from "../service/notificationService";
import type { NotificationPage } from "../types/notificationTypes";

export function notificationListQueryKey(userId: string | null) {
  return [
    NotificationQueryScope.root,
    userId,
    NotificationQueryScope.list,
  ] as const;
}

async function loadPage(cursor: string | null): Promise<NotificationPage> {
  const outcome = await getNotifications(cursor);

  if (outcome.kind !== "answer") {
    // Thrown so the query reports an error and the panel can offer a retry.
    throw new Error("The notifications could not be loaded.");
  }

  return outcome.value;
}

export function useNotificationList(isOpen: boolean) {
  const { status, userId } = useAuth();

  return useInfiniteQuery({
    queryKey: notificationListQueryKey(userId),
    queryFn: ({ pageParam }) => loadPage(pageParam),
    initialPageParam: null as string | null,
    getNextPageParam: (lastPage) => lastPage.nextCursor,
    enabled: isOpen && status === "member" && userId !== null,
    staleTime: NotificationCache.listStaleMs,
  });
}
