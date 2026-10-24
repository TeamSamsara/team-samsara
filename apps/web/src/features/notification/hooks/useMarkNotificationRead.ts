// File : /team-samsara/apps/web/src/features/notification/hooks/useMarkNotificationRead.ts
// Version : 1.1.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Marks one notification, or all of them, as read. The list and the
// unread count change at once, then roll back if the request fails. Either way
// the cache is refreshed from the server afterwards.

import { useMutation } from "@tanstack/react-query";
import {
  markAllNotificationsRead,
  markNotificationRead,
} from "../service/notificationService";
import {
  useNotificationCache,
  type CacheSnapshot,
} from "./useNotificationCache";

export function useMarkNotificationRead() {
  const cache = useNotificationCache();

  return useMutation<void, Error, string, CacheSnapshot>({
    mutationFn: async (id) => {
      const outcome = await markNotificationRead(id);

      if (outcome.kind !== "answer") {
        throw new Error("The notification could not be marked as read.");
      }
    },
    onMutate: async (id) => {
      const previous = await cache.snapshot();
      cache.markOneInCache(id);

      return previous;
    },
    onError: (_error, _id, previous) => cache.restore(previous),
    onSettled: () => cache.refresh(),
  });
}

export function useMarkAllNotificationsRead() {
  const cache = useNotificationCache();

  return useMutation<void, Error, void, CacheSnapshot>({
    mutationFn: async () => {
      const outcome = await markAllNotificationsRead();

      if (outcome.kind !== "answer") {
        throw new Error("The notifications could not be marked as read.");
      }
    },
    onMutate: async () => {
      const previous = await cache.snapshot();
      cache.markAllInCache();

      return previous;
    },
    onError: (_error, _variables, previous) => cache.restore(previous),
    onSettled: () => cache.refresh(),
  });
}
