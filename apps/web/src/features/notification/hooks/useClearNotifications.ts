// File : /team-samsara/apps/web/src/features/notification/hooks/useClearNotifications.ts
// Version : 1.0.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Removes one notification, or all the read ones. The list changes
// at once and rolls back with an error toast if the request fails. Either way
// the cache is refreshed from the server afterwards.

import { useMutation } from "@tanstack/react-query";
import { useToast } from "@/shared/components/Toast/useToast";
import {
  clearReadNotifications,
  removeNotification,
} from "../service/notificationService";
import { NotificationText } from "../view/notificationContent";
import {
  useNotificationCache,
  type CacheSnapshot,
} from "./useNotificationCache";

export function useRemoveNotification() {
  const cache = useNotificationCache();
  const { show } = useToast();

  return useMutation<void, Error, string, CacheSnapshot>({
    mutationFn: async (id) => {
      const outcome = await removeNotification(id);

      if (outcome.kind !== "answer") {
        throw new Error("The notification could not be removed.");
      }
    },
    onMutate: async (id) => {
      const previous = await cache.snapshot();
      cache.removeOneFromCache(id);

      return previous;
    },
    onError: (_error, _id, previous) => {
      cache.restore(previous);
      show({ tone: "error", title: NotificationText.panel.removeFailed });
    },
    onSettled: () => cache.refresh(),
  });
}

export function useClearReadNotifications() {
  const cache = useNotificationCache();
  const { show } = useToast();

  return useMutation<void, Error, void, CacheSnapshot>({
    mutationFn: async () => {
      const outcome = await clearReadNotifications();

      if (outcome.kind !== "answer") {
        throw new Error("The read notifications could not be cleared.");
      }
    },
    onMutate: async () => {
      const previous = await cache.snapshot();
      cache.removeReadFromCache();

      return previous;
    },
    onError: (_error, _variables, previous) => {
      cache.restore(previous);
      show({ tone: "error", title: NotificationText.panel.clearReadFailed });
    },
    onSettled: () => cache.refresh(),
  });
}
