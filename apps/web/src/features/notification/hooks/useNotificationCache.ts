// File : /team-samsara/apps/web/src/features/notification/hooks/useNotificationCache.ts
// Version : 1.0.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : The member's cached notification list and unread count, with the
// steps every inbox change needs: take a snapshot, change the cache at once
// (mark read, remove, clear the read ones), put the snapshot back if the
// request fails, and refresh from the server afterwards.

import { useQueryClient, type InfiniteData } from "@tanstack/react-query";
import { useAuth } from "@/app/useAuth";
import { NotificationQueryScope } from "../constants/notificationKeys";
import type {
  NotificationItem,
  NotificationPage,
} from "../types/notificationTypes";
import { notificationListQueryKey } from "./useNotificationList";
import { unreadCountQueryKey } from "./useUnreadCount";

type ListData = InfiniteData<NotificationPage, string | null>;

export interface CacheSnapshot {
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

function keepItems(
  data: ListData | undefined,
  keep: (item: NotificationItem) => boolean,
): ListData | undefined {
  if (data === undefined) {
    return data;
  }

  return {
    ...data,
    pages: data.pages.map((page) => ({
      ...page,
      items: page.items.filter(keep),
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

export function useNotificationCache() {
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

  function countOneLess(): void {
    queryClient.setQueryData<number>(countKey, (old) =>
      old === undefined ? old : Math.max(0, old - 1),
    );
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
      countOneLess();
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

  // An unread notice that goes away also leaves the unread count.
  function removeOneFromCache(id: string): void {
    const wasUnread = isUnreadIn(
      queryClient.getQueryData<ListData>(listKey),
      id,
    );

    queryClient.setQueryData<ListData>(listKey, (old) =>
      keepItems(old, (item) => item.id !== id),
    );

    if (wasUnread) {
      countOneLess();
    }
  }

  // Only read notifications go, so the unread count stays as it is.
  function removeReadFromCache(): void {
    queryClient.setQueryData<ListData>(listKey, (old) =>
      keepItems(old, (item) => item.readAt === null),
    );
  }

  function refresh(): Promise<void> {
    return queryClient.invalidateQueries({ queryKey: rootKey });
  }

  return {
    snapshot,
    restore,
    markOneInCache,
    markAllInCache,
    removeOneFromCache,
    removeReadFromCache,
    refresh,
  };
}
