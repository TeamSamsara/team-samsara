// File : /team-samsara/apps/web/src/features/notification/hooks/useArrivingNotifications.ts
// Version : 1.0.0
// Latest commit: task/web-notifications
// Author : Gerrah
// Purpose : Tells the panel which notifications arrived while it was open, so
// only those grow in and flash. Rows from the first load, rows added by "load
// more" (older ones) and anything that came in while the panel was closed
// are not arrivals.

import { useState } from "react";
import type { NotificationItem } from "../types/notificationTypes";

interface ArrivalState {
  readonly hasBaseline: boolean;
  readonly knownIds: ReadonlySet<string>;
  readonly newestTime: number;
  readonly arrivingIds: ReadonlySet<string>;
}

const NO_IDS: ReadonlySet<string> = new Set();

const EMPTY_STATE: ArrivalState = {
  hasBaseline: false,
  knownIds: NO_IDS,
  newestTime: 0,
  arrivingIds: NO_IDS,
};

function timeOf(item: NotificationItem): number {
  return Date.parse(item.createdAt) || 0;
}

function newestOf(items: readonly NotificationItem[]): number {
  return items.reduce((newest, item) => Math.max(newest, timeOf(item)), 0);
}

function takeSnapshot(items: readonly NotificationItem[]): ArrivalState {
  return {
    hasBaseline: true,
    knownIds: new Set(items.map((item) => item.id)),
    newestTime: newestOf(items),
    arrivingIds: NO_IDS,
  };
}

function isUnchanged(
  state: ArrivalState,
  items: readonly NotificationItem[],
): boolean {
  return (
    state.hasBaseline &&
    state.arrivingIds.size === 0 &&
    state.knownIds.size === items.length &&
    items.every((item) => state.knownIds.has(item.id))
  );
}

function withArrivals(
  state: ArrivalState,
  items: readonly NotificationItem[],
): ArrivalState {
  const fresh = items.filter((item) => !state.knownIds.has(item.id));
  if (fresh.length === 0) return state;

  const arriving = fresh.filter((item) => timeOf(item) > state.newestTime);

  return {
    hasBaseline: true,
    knownIds: new Set([...state.knownIds, ...fresh.map((item) => item.id)]),
    newestTime: Math.max(state.newestTime, newestOf(fresh)),
    arrivingIds: new Set([
      ...state.arrivingIds,
      ...arriving.map((item) => item.id),
    ]),
  };
}

function nextState(
  state: ArrivalState,
  items: readonly NotificationItem[],
  isOpen: boolean,
  isLoaded: boolean,
): ArrivalState {
  if (!isLoaded) return state;

  if (!state.hasBaseline || !isOpen) {
    return isUnchanged(state, items) ? state : takeSnapshot(items);
  }

  return withArrivals(state, items);
}

export function useArrivingNotifications(
  items: readonly NotificationItem[],
  isOpen: boolean,
  isLoaded: boolean,
): ReadonlySet<string> {
  const [state, setState] = useState(EMPTY_STATE);

  // Adjusted while rendering, the documented way to derive state from props,
  // so a new row is flagged in the same render it first appears in.
  const next = nextState(state, items, isOpen, isLoaded);
  if (next !== state) setState(next);

  return next.arrivingIds;
}
