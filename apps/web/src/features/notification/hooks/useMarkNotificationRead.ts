// File : /team-samsara/apps/web/src/features/notifications/hooks/useMarkNotificationRead.ts
// Version : 1.0.0
// Latest commit: task/web-notifications
// Author : Gerrah
// Purpose : Marks one notification, or all of them, as read. The list and the
// unread count change at once, then roll back if the request fails. Either way
// the cache is refreshed from the server afterwards.

import {
  useMutation,
  useQueryClient,
  type InfiniteData,
} from "@tanstack/react-query";
import { useAuth } from "@/app/useAuth";
import { NotificationQueryScope } from "../constants/notificationKeys";
import {
  markAllNotificationsRead,
  markNotificationRead,
} from "../service/notificationService";
import type {
  NotificationItem,
  NotificationPage,
} from "../types/notificationTypes";
import { notificationListQueryKey } from "./useNotificationList";
import { unreadCountQueryKey } from "./useUnreadCount";

type ListData = InfiniteData<NotificationPage, string | null>;

interface CacheSnapshot {
  readonly list: ListData | undefined;
  readonly count: number | undefined;
}

function changeItems(
  data: ListData | undefined,
  change: (item: NotificationItem) => NotificationItem,
): ListData | undefined {
  if (data === undefined) {
    return data;
  }

  return {
    ...data,
    pages: data.pages.map((page) => ({
      ...page,
      items: page.items.map(change),
    })),
  };
}

function isUnreadIn(data: ListData | undefined, id: string): boolean {
  return (
    data?.pages.some((page) =>
      page.items.some((item) => item.id === id && item.readAt === null),
    ) ?? false
  );
}

function withReadAt(item: NotificationItem, readAt: string): NotificationItem {
  return item.readAt === null ? { ...item, readAt } : item;
}

function useNotificationCache() {
  const queryClient = useQueryClient();
  const { userId } = useAuth();

  const listKey = notificationListQueryKey(userId);
  const countKey = unreadCountQueryKey(userId);
  const rootKey = [NotificationQueryScope.root, userId] as const;

  async function snapshot(): Promise<CacheSnapshot> {
    await queryClient.cancelQueries({ queryKey: rootKey });

    return {
      list: queryClient.getQueryData<ListData>(listKey),
      count: queryClient.getQueryData<number>(countKey),
    };
  }

  function restore(previous: CacheSnapshot | undefined): void {
    if (previous === undefined) {
      return;
    }

    queryClient.setQueryData(listKey, previous.list);
    queryClient.setQueryData(countKey, previous.count);
  }

  function markOneInCache(id: string): void {
    const wasUnread = isUnreadIn(
      queryClient.getQueryData<ListData>(listKey),
      id,
    );
    const readAt = new Date().toISOString();

    queryClient.setQueryData<ListData>(listKey, (old) =>
      changeItems(old, (item) =>
        item.id === id ? withReadAt(item, readAt) : item,
      ),
    );

    if (wasUnread) {
      queryClient.setQueryData<number>(countKey, (old) =>
        old === undefined ? old : Math.max(0, old - 1),
      );
    }
  }

  function markAllInCache(): void {
    const readAt = new Date().toISOString();

    queryClient.setQueryData<ListData>(listKey, (old) =>
      changeItems(old, (item) => withReadAt(item, readAt)),
    );
    queryClient.setQueryData<number>(countKey, (old) =>
      old === undefined ? old : 0,
    );
  }

  function refresh(): Promise<void> {
    return queryClient.invalidateQueries({ queryKey: rootKey });
  }

  return { snapshot, restore, markOneInCache, markAllInCache, refresh };
}

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
