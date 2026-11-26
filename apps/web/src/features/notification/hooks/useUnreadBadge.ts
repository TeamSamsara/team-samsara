// File : /team-samsara/apps/web/src/features/notification/hooks/useUnreadBadge.ts
// Version : 1.0.0
// Latest commit: task/web-notifications
// Author : Gerrah
// Purpose : Turns the unread count into what the bell badge needs to animate:
// whether it shows, its label, the label it replaces, which way the digit
// rolls, and an arrival counter that goes up each time something new comes in.

import { useState } from "react";
import { NotificationText } from "../view/notificationContent";

export type BadgeDirection = "up" | "down";

export interface UnreadBadgeState {
  readonly count: number | null;
  readonly isVisible: boolean;
  readonly label: string;
  readonly previousLabel: string | null;
  readonly direction: BadgeDirection;
  readonly isAppearing: boolean;
  readonly arrival: number;
}

function start(count: number | undefined): UnreadBadgeState {
  const known = count ?? null;
  const isVisible = known !== null && known > 0;

  return {
    count: known,
    isVisible,
    label: isVisible ? NotificationText.bell.badge(known) : "",
    previousLabel: null,
    direction: "up",
    isAppearing: false,
    arrival: 0,
  };
}

function hide(state: UnreadBadgeState): UnreadBadgeState {
  return {
    ...state,
    count: 0,
    isVisible: false,
    previousLabel: null,
    isAppearing: false,
  };
}

function change(state: UnreadBadgeState, count: number): UnreadBadgeState {
  const label = NotificationText.bell.badge(count);
  const isAppearing = state.count === 0;
  const hasRisen = state.count !== null && count > state.count;
  const rolls = !isAppearing && label !== state.label;

  return {
    count,
    isVisible: true,
    label,
    previousLabel: rolls ? state.label : null,
    direction: hasRisen ? "up" : "down",
    isAppearing,
    arrival: hasRisen ? state.arrival + 1 : state.arrival,
  };
}

function nextState(
  state: UnreadBadgeState,
  count: number | undefined,
): UnreadBadgeState {
  if (count === undefined || count === state.count) return state;
  if (state.count === null) return start(count);
  if (count === 0) return hide(state);
  return change(state, count);
}

export function useUnreadBadge(count: number | undefined): UnreadBadgeState {
  const [state, setState] = useState(() => start(count));

  // Adjusted while rendering, the documented way to derive state from a prop,
  // so the badge never paints one frame with the old number.
  const next = nextState(state, count);
  if (next !== state) setState(next);

  return next;
}
