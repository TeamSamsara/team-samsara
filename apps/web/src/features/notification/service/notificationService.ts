// File : /team-samsara/apps/web/src/features/notification/service/notificationService.ts
// Version : 1.1.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Every call to the notification inbox API, one function per step,
// including removing one notification and clearing all the read ones.
// Each returns an ApiOutcome and never throws. An item the app cannot read is
// skipped, so one odd notification never blanks the whole inbox.

import {
  isObject,
  parseNoContent,
  postEmpty,
  sendAccountRequest,
  type ApiOutcome,
} from "@/features/auth/service/accountOutcome";
import { NotificationApiRoutes } from "../constants/notificationApiRoutes";
import { NotificationPaging } from "../constants/notificationKeys";
import type {
  NotificationItem,
  NotificationPage,
} from "../types/notificationTypes";

// ---- Parsers ----

function readData(value: unknown): Record<string, string> | null {
  if (!isObject(value)) {
    return null;
  }

  const data: Record<string, string> = {};

  for (const [name, entry] of Object.entries(value)) {
    if (typeof entry === "string") {
      data[name] = entry;
    }
  }

  return data;
}

function readItem(raw: unknown): NotificationItem | null {
  if (
    !isObject(raw) ||
    typeof raw.id !== "string" ||
    typeof raw.key !== "string" ||
    typeof raw.createdAt !== "string"
  ) {
    return null;
  }

  const data = readData(raw.data ?? {});
  const readAt = raw.readAt ?? null;

  if (data === null || (readAt !== null && typeof readAt !== "string")) {
    return null;
  }

  return {
    id: raw.id,
    key: raw.key,
    data,
    createdAt: raw.createdAt,
    readAt,
  };
}

function readItems(raw: readonly unknown[]): NotificationItem[] {
  const items: NotificationItem[] = [];

  for (const entry of raw) {
    const item = readItem(entry);

    if (item !== null) {
      items.push(item);
    }
  }

  return items;
}

function parseNotificationPage(body: unknown): NotificationPage | null {
  if (!isObject(body) || !Array.isArray(body.items)) {
    return null;
  }

  const nextCursor = body.nextCursor ?? null;

  if (nextCursor !== null && typeof nextCursor !== "string") {
    return null;
  }

  return { items: readItems(body.items), nextCursor };
}

function parseUnreadCount(body: unknown): number | null {
  if (
    !isObject(body) ||
    typeof body.unreadCount !== "number" ||
    !Number.isInteger(body.unreadCount) ||
    body.unreadCount < 0
  ) {
    return null;
  }

  return body.unreadCount;
}

const deleteEmpty: RequestInit = { method: "DELETE" };

// ---- Requests ----

function listPath(cursor: string | null): string {
  const query = new URLSearchParams({
    limit: String(NotificationPaging.pageSize),
  });

  if (cursor !== null) {
    query.set("cursor", cursor);
  }

  return `${NotificationApiRoutes.list}?${query.toString()}`;
}

// ---- Inbox ----

export function getNotifications(
  cursor: string | null,
): Promise<ApiOutcome<NotificationPage>> {
  return sendAccountRequest(listPath(cursor), parseNotificationPage);
}

export function getUnreadCount(): Promise<ApiOutcome<number>> {
  return sendAccountRequest(
    NotificationApiRoutes.unreadCount,
    parseUnreadCount,
  );
}

export function markNotificationRead(id: string): Promise<ApiOutcome<true>> {
  return sendAccountRequest(
    NotificationApiRoutes.read(id),
    parseNoContent,
    postEmpty,
  );
}

export function markAllNotificationsRead(): Promise<ApiOutcome<true>> {
  return sendAccountRequest(
    NotificationApiRoutes.readAll,
    parseNoContent,
    postEmpty,
  );
}

// Answers "unknown" for a notice that cannot be removed (an unanswered sign-in) or is already gone.
export function removeNotification(id: string): Promise<ApiOutcome<true>> {
  return sendAccountRequest(
    NotificationApiRoutes.remove(id),
    parseNoContent,
    deleteEmpty,
  );
}

export function clearReadNotifications(): Promise<ApiOutcome<true>> {
  return sendAccountRequest(
    NotificationApiRoutes.clearRead,
    parseNoContent,
    deleteEmpty,
  );
}
