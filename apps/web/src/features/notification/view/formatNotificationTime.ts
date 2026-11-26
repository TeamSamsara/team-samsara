// File : /team-samsara/apps/web/src/features/notification/view/formatNotificationTime.ts
// Version : 1.0.0
// Latest commit: task/web-notifications
// Author : Gerrah
// Purpose : Turns a notification's ISO date into the wording shown to the
// member: "12 minutes ago" or "Oct 3" in the list, "today at 12:41" in the
// banner. An unreadable date gives an empty string.

import { NotificationText } from "./notificationContent";

const MinuteMs = 60_000;
const HourMs = 60 * MinuteMs;
const DayMs = 24 * HourMs;

const relativeFormat = new Intl.RelativeTimeFormat("en", { numeric: "always" });
const dayFormat = new Intl.DateTimeFormat("en", {
  month: "short",
  day: "numeric",
});
const dayWithYearFormat = new Intl.DateTimeFormat("en", {
  month: "short",
  day: "numeric",
  year: "numeric",
});
const timeFormat = new Intl.DateTimeFormat("en", {
  hour: "2-digit",
  minute: "2-digit",
  hourCycle: "h23",
});

function parseDate(iso: string): Date | null {
  const date = new Date(iso);

  return Number.isNaN(date.getTime()) ? null : date;
}

function startOfDay(date: Date): number {
  return new Date(
    date.getFullYear(),
    date.getMonth(),
    date.getDate(),
  ).getTime();
}

function daysBetween(then: Date, now: Date): number {
  return Math.round((startOfDay(now) - startOfDay(then)) / DayMs);
}

function formatDay(date: Date, now: Date): string {
  return date.getFullYear() === now.getFullYear()
    ? dayFormat.format(date)
    : dayWithYearFormat.format(date);
}

export function formatRelativeTime(
  iso: string,
  now: Date = new Date(),
): string {
  const date = parseDate(iso);

  if (date === null) {
    return "";
  }

  const elapsed = now.getTime() - date.getTime();

  if (elapsed < MinuteMs) {
    return NotificationText.time.justNow;
  }

  if (elapsed < HourMs) {
    return relativeFormat.format(-Math.floor(elapsed / MinuteMs), "minute");
  }

  if (elapsed < DayMs) {
    return relativeFormat.format(-Math.floor(elapsed / HourMs), "hour");
  }

  return formatDay(date, now);
}

export function formatBannerTime(iso: string, now: Date = new Date()): string {
  const date = parseDate(iso);

  if (date === null) {
    return "";
  }

  const time = timeFormat.format(date);
  const days = daysBetween(date, now);

  if (days === 0) {
    return NotificationText.time.todayAt(time);
  }

  if (days === 1) {
    return NotificationText.time.yesterdayAt(time);
  }

  return `${formatDay(date, now)} ${time}`;
}
