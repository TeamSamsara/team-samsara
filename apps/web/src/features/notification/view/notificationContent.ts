// File : /team-samsara/apps/web/src/features/notification/view/notificationContent.ts
// Version : 1.1.0
// Latest commit: task/web-notifications
// Author : Gerrah
// Purpose : All the text of the notification bell, panel, sign-in banner and
// "secure your account" modal, including the wording built from live values.

import type { ClientDescription } from "./describeClient";

export const NotificationText = {
  bell: {
    label: "Notifications",
    labelWithUnread: (count: number) => `Notifications, ${count} unread`,
    badge: (count: number) => (count > 9 ? "9+" : String(count)),
  },

  panel: {
    title: "NOTIFICATIONS",
    period: "Last 90 days",
    markAllRead: "MARK ALL AS READ",
    markRead: "MARK AS READ",
    loadMore: "LOAD MORE",
    loadingMore: "LOADING…",
    emptyTitle: "YOU ARE ALL CAUGHT UP",
    emptyMessage:
      "Security alerts and updates about your account show up here.",
    errorTitle: "COULD NOT LOAD NOTIFICATIONS",
    errorMessage: "Check your connection and try again.",
    retry: "TRY AGAIN",
  },

  signIn: {
    title: "NEW SIGN-IN ATTEMPT",
    detail: (client: string, ipAddress: string | null) =>
      ipAddress === null ? client : `${client}, IP ${ipAddress}`,
  },

  fallback: {
    title: "NEW NOTIFICATION",
    detail: "There is something new on your account.",
  },

  client: {
    unknown: "Unknown device",
    describe: ({ browser, system }: ClientDescription) => {
      if (browser !== null && system !== null) {
        return `${browser} on ${system}`;
      }

      return browser ?? (system !== null ? `Device on ${system}` : null);
    },
  },

  time: {
    justNow: "just now",
    todayAt: (time: string) => `today at ${time}`,
    yesterdayAt: (time: string) => `yesterday at ${time}`,
  },

  banner: {
    title: "NEW SIGN-IN ATTEMPT ON YOUR ACCOUNT",
    detail: (signIn: string, when: string) =>
      `${signIn}, ${when}. They still need your email code to get in.`,
    thatWasMe: "THAT WAS ME",
    secureMyAccount: "SECURE MY ACCOUNT",
  },

  secure: {
    title: "SECURE YOUR ACCOUNT?",
    description:
      "This signs you out on every device, this one included, and the code the other device was sent stops working. Then you choose a new password.",
    note: "Do this if you do not recognise the sign-in.",
    confirm: "SIGN OUT EVERYWHERE",
    cancel: "NOT NOW",
  },
} as const;
