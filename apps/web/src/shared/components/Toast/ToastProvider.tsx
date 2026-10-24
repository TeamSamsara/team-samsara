// File : /team-samsara/apps/web/src/shared/components/Toast/ToastProvider.tsx
// Version : 1.0.0
// Latest commit: task/web-toast-messages
// Author : Gerrah
// Purpose : Holds the list of toasts and the actions to change it, and draws the viewport. At most three
// stay on screen, the oldest leaves first, and the same message sent again replaces its earlier copy.

import { useMemo, useRef, useState, type ReactNode } from "react";
import { ToastActionsContext, ToastListContext } from "./ToastContext";
import { maxVisibleToasts, toastDuration } from "./toastTiming";
import type { ToastActions, ToastInput, ToastItem } from "./toastTypes";
import { ToastViewport } from "./ToastViewport";

interface ToastProviderProps {
  children: ReactNode;
}

function isSameMessage(toast: ToastItem, input: ToastInput): boolean {
  return (
    toast.tone === input.tone &&
    toast.title === input.title &&
    toast.detail === input.detail
  );
}

function markLeaving(
  toasts: readonly ToastItem[],
  shouldLeave: (toast: ToastItem) => boolean,
): readonly ToastItem[] {
  const needsChange = toasts.some(
    (toast) => !toast.isLeaving && shouldLeave(toast),
  );
  if (!needsChange) return toasts;
  return toasts.map((toast) =>
    !toast.isLeaving && shouldLeave(toast)
      ? { ...toast, isLeaving: true }
      : toast,
  );
}

function evictOldest(toasts: readonly ToastItem[]): readonly ToastItem[] {
  const active = toasts.filter((toast) => !toast.isLeaving);
  const overflow = active.length - maxVisibleToasts;
  if (overflow <= 0) return toasts;
  const evicted = new Set(active.slice(0, overflow).map((toast) => toast.id));
  return markLeaving(toasts, (toast) => evicted.has(toast.id));
}

function addToast(
  toasts: readonly ToastItem[],
  input: ToastInput,
  item: ToastItem,
): readonly ToastItem[] {
  const withoutCopy = markLeaving(toasts, (toast) =>
    isSameMessage(toast, input),
  );
  return evictOldest([...withoutCopy, item]);
}

function buildItem(input: ToastInput, id: string): ToastItem {
  const text = `${input.title} ${input.detail ?? ""}`;
  return {
    ...input,
    id,
    durationMs: toastDuration(input.tone, text),
    isLeaving: false,
  };
}

export function ToastProvider({ children }: ToastProviderProps) {
  const [toasts, setToasts] = useState<readonly ToastItem[]>([]);
  const counter = useRef(0);

  const actions = useMemo<ToastActions>(
    () => ({
      show(input) {
        counter.current += 1;
        const id = `toast-${counter.current}`;
        setToasts((current) => addToast(current, input, buildItem(input, id)));
        return id;
      },
      dismiss(id) {
        setToasts((current) =>
          markLeaving(current, (toast) => toast.id === id),
        );
      },
      remove(id) {
        setToasts((current) => current.filter((toast) => toast.id !== id));
      },
    }),
    [],
  );

  return (
    <ToastActionsContext value={actions}>
      <ToastListContext value={toasts}>
        {children}
        <ToastViewport />
      </ToastListContext>
    </ToastActionsContext>
  );
}
