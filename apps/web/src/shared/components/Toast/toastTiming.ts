// File : /team-samsara/apps/web/src/shared/components/Toast/toastTiming.ts
// Version : 1.0.0
// Latest commit: task/web-toast-messages
// Author : Gerrah
// Purpose : How long a toast stays on screen and how many show at once.

import type { ToastTone } from "./toastTypes";

export const maxVisibleToasts = 3;

const baseDurationMs: Record<ToastTone, number> = {
  success: 4000,
  info: 6000,
  warning: 7000,
  error: 8000,
};

const freeCharacters = 40;
const msPerExtraCharacter = 40;
const maxDurationMs = 12000;

function extraReadingTime(text: string): number {
  return Math.max(0, text.length - freeCharacters) * msPerExtraCharacter;
}

export function toastDuration(tone: ToastTone, text: string): number {
  return Math.min(maxDurationMs, baseDurationMs[tone] + extraReadingTime(text));
}
