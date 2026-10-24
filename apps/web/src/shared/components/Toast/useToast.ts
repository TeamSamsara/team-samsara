// File : /team-samsara/apps/web/src/shared/components/Toast/useToast.ts
// Version : 1.0.0
// Latest commit: task/web-toast-messages
// Author : Gerrah
// Purpose : Reads the toast contexts. useToast gives screens show/dismiss and never re-renders when the
// list changes; useToastList gives the viewport the toasts to draw.

import { useContext } from "react";
import { DevInvariants } from "@/lib/devInvariants";
import { ToastActionsContext, ToastListContext } from "./ToastContext";
import type { ToastActions, ToastItem } from "./toastTypes";

export function useToast(): ToastActions {
  const actions = useContext(ToastActionsContext);
  if (!actions) {
    throw new Error(DevInvariants.useToastOutsideProvider);
  }
  return actions;
}

export function useToastList(): readonly ToastItem[] {
  const list = useContext(ToastListContext);
  if (!list) {
    throw new Error(DevInvariants.useToastOutsideProvider);
  }
  return list;
}
