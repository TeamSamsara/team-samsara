// File : /team-samsara/apps/web/src/features/notification/ui/NotificationIconButton.tsx
// Version : 1.0.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : A small icon-only button for the notification panel. The icon
// carries the meaning for sighted people; the label is its accessible name and
// its hover tooltip.

import { Icon } from "@/shared/icons/Icon";
import type { IconName } from "@/shared/icons/iconRegistry";
import styles from "./NotificationIconButton.module.css";

interface NotificationIconButtonProps {
  readonly icon: IconName;
  /** What the button does; read by screen readers and shown on hover. */
  readonly label: string;
  readonly onClick: () => void;
  readonly disabled?: boolean;
  /** "quiet" for the less important action, such as removing a notice. */
  readonly tone?: "accent" | "quiet";
}

export function NotificationIconButton({
  icon,
  label,
  onClick,
  disabled = false,
  tone = "accent",
}: NotificationIconButtonProps) {
  return (
    <button
      type="button"
      className={styles.button}
      data-tone={tone}
      aria-label={label}
      title={label}
      disabled={disabled}
      onClick={onClick}
    >
      <Icon name={icon} className={styles.icon} strokeWidth={2.4} />
    </button>
  );
}
