// File : /team-samsara/apps/web/src/shared/components/Toast/Toast.tsx
// Version : 1.0.0
// Latest commit: task/web-toast-messages
// Author : Gerrah
// Purpose : Draws one toast. Its timer line asks to dismiss when it runs out, and the toast asks to
// be removed once its leaving animation has finished.

import type { AnimationEvent, CSSProperties } from "react";
import { Icon } from "@/shared/icons/Icon";
import type { IconName } from "@/shared/icons/iconRegistry";
import type { ToastItem, ToastTone } from "./toastTypes";
import styles from "./Toast.module.css";

const closeLabel = "Close";

const toneIcon: Record<ToastTone, IconName> = {
  success: "check",
  info: "info",
  warning: "warning",
  error: "error",
};

interface ToastProps {
  toast: ToastItem;
  onDismiss: (id: string) => void;
  onRemove: (id: string) => void;
}

function roleFor(tone: ToastTone): "alert" | "status" {
  return tone === "error" ? "alert" : "status";
}

export function Toast({ toast, onDismiss, onRemove }: ToastProps) {
  const lifeStyle = {
    "--toast-life": `${toast.durationMs}ms`,
  } as CSSProperties;

  function handleCollapseEnd(event: AnimationEvent<HTMLDivElement>) {
    const isOwnAnimation = event.target === event.currentTarget;
    if (isOwnAnimation && toast.isLeaving) onRemove(toast.id);
  }

  return (
    <div
      className={styles.collapse}
      data-leaving={toast.isLeaving}
      onAnimationEnd={handleCollapseEnd}
    >
      <div className={styles.slot}>
        <div
          className={styles.toast}
          data-tone={toast.tone}
          role={roleFor(toast.tone)}
          style={lifeStyle}
        >
          <Icon name={toneIcon[toast.tone]} className={styles.icon} />
          <div className={styles.text}>
            <p className={styles.title}>{toast.title}</p>
            {toast.detail ? (
              <p className={styles.detail}>{toast.detail}</p>
            ) : null}
          </div>
          <button
            type="button"
            className={styles.close}
            aria-label={closeLabel}
            onClick={() => onDismiss(toast.id)}
          >
            <Icon name="close" className={styles.closeIcon} strokeWidth={2.5} />
          </button>
          <div
            className={styles.bar}
            aria-hidden
            onAnimationEnd={() => onDismiss(toast.id)}
          />
        </div>
      </div>
    </div>
  );
}
