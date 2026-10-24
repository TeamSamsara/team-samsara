// File : /team-samsara/apps/web/src/shared/components/Toast/ToastContext.ts
// Version : 1.0.0
// Latest commit: task/web-toast-messages
// Author : Gerrah
// Purpose : The two toast contexts, shared by ToastProvider (which fills them) and the hooks
// that read them. Kept apart from the provider so Fast Refresh keeps working.

import { createContext } from "react";
import type { ToastActions, ToastItem } from "./toastTypes";

export const ToastActionsContext = createContext<ToastActions | undefined>(
  undefined,
);

export const ToastListContext = createContext<readonly ToastItem[] | undefined>(
  undefined,
);
