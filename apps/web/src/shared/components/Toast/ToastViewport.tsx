// File : /team-samsara/apps/web/src/shared/components/Toast/ToastViewport.tsx
// Version : 1.0.0
// Latest commit: task/web-toast-messages
// Author : Gerrah
// Purpose : Draws the toast stack in the top-right corner, one Toast per message in the list.

import { Toast } from "./Toast";
import { useToast, useToastList } from "./useToast";
import styles from "./ToastViewport.module.css";

const viewportLabel = "Messages";

export function ToastViewport() {
  const toasts = useToastList();
  const { dismiss, remove } = useToast();

  return (
    <div className={styles.viewport} role="region" aria-label={viewportLabel}>
      {toasts.map((toast) => (
        <Toast
          key={toast.id}
          toast={toast}
          onDismiss={dismiss}
          onRemove={remove}
        />
      ))}
    </div>
  );
}
