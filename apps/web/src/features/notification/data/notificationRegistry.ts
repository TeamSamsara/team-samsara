// File : /team-samsara/apps/web/src/features/notification/data/notificationRegistry.ts
// Version : 1.0.0
// Latest commit: task/web-notifications
// Author : Gerrah
// Purpose : Maps each notification key to the icon, title and detail the
// member sees. A key the app does not know still shows a generic notice, so a
// new backend notification never breaks the panel. Data values are shown as
// plain text only.

import type { IconName } from "@/shared/icons/iconRegistry";
import { NotificationKeys } from "../constants/notificationKeys";
import type { NotificationItem } from "../types/notificationTypes";
import { describeClient } from "../view/describeClient";
import { NotificationText } from "../view/notificationContent";

export interface NotificationPresentation {
  readonly icon: IconName;
  readonly title: string;
  readonly detail: string;
}

type NotificationData = NotificationItem["data"];

function readText(data: NotificationData, name: string): string | null {
  const value = data[name];

  return typeof value === "string" && value.trim() !== "" ? value : null;
}

export function signInSummary(data: NotificationData): string {
  const client =
    NotificationText.client.describe(
      describeClient(readText(data, "userAgent") ?? undefined),
    ) ?? NotificationText.client.unknown;

  return NotificationText.signIn.detail(client, readText(data, "ipAddress"));
}

function presentNewLogin(data: NotificationData): NotificationPresentation {
  return {
    icon: "warning",
    title: NotificationText.signIn.title,
    detail: signInSummary(data),
  };
}

const fallbackPresentation: NotificationPresentation = {
  icon: "info",
  title: NotificationText.fallback.title,
  detail: NotificationText.fallback.detail,
};

// A Map, not an object: a key such as "constructor" must never find a builder.
const presenters = new Map<
  string,
  (data: NotificationData) => NotificationPresentation
>([[NotificationKeys.newLogin, presentNewLogin]]);

export function presentNotification(
  item: NotificationItem,
): NotificationPresentation {
  const present = presenters.get(item.key);

  return present === undefined ? fallbackPresentation : present(item.data);
}
