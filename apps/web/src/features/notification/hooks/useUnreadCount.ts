// File : /team-samsara/apps/web/src/features/notification/hooks/useUnreadCount.ts
// Version : 1.1.0
// Latest commit: task/web-notifications
// Author : Gerrah
// Purpose : Reads the signed-in member's unread notification count for the
// bell and the banner. It refreshes when the window regains focus and every
// few seconds while the tab is visible, so a new notice shows up on its own.
// The cache is keyed by user id, so a different member never sees the
// previous member's count.

import { useQuery } from "@tanstack/react-query";
import { useAuth } from "@/app/useAuth";
import {
  NotificationCache,
  NotificationQueryScope,
} from "../constants/notificationKeys";
import { getUnreadCount } from "../service/notificationService";

export function unreadCountQueryKey(userId: string | null) {
  return [
    NotificationQueryScope.root,
    userId,
    NotificationQueryScope.unreadCount,
  ] as const;
}

async function loadUnreadCount(): Promise<number> {
  const outcome = await getUnreadCount();

  if (outcome.kind !== "answer") {
    // Thrown so the query reports an error and can try again later.
    throw new Error("The unread count could not be loaded.");
  }

  return outcome.value;
}

export function useUnreadCount() {
  const { status, userId } = useAuth();

  return useQuery({
    queryKey: unreadCountQueryKey(userId),
    queryFn: loadUnreadCount,
    enabled: status === "member" && userId !== null,
    staleTime: NotificationCache.unreadCountStaleMs,
    refetchOnWindowFocus: true,
    refetchInterval: NotificationCache.unreadCountPollMs,
    refetchIntervalInBackground: false,
  });
}
