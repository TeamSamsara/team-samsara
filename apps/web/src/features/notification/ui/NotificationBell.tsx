// File : /team-samsara/apps/web/src/features/notification/ui/NotificationBell.tsx
// Version : 1.3.1
// Latest commit: task/web-notifications
// Author : Gerrah
// Purpose : The bell in the navbar. It shows an amber badge with the unread
// count that pops in, rolls its digit when the count changes and swings the
// bell when something new arrives. It opens the notification panel, keeps the
// list fresh when new notices come in, and owns the "secure your account"
// modal, because the panel closes whenever focus leaves it.

import { useState } from "react";
import { Dropdown } from "@/shared/components/Dropdown/Dropdown";
import { DropdownPanel } from "@/shared/components/Dropdown/DropdownPanel";
import { DropdownTrigger } from "@/shared/components/Dropdown/DropdownTrigger";
import { useDropdownContext } from "@/shared/components/Dropdown/useDropdownContext";
import { Icon } from "@/shared/icons/Icon";
import { useRefreshOnNewNotifications } from "../hooks/useRefreshOnNewNotifications";
import { useUnreadBadge, type UnreadBadgeState } from "../hooks/useUnreadBadge";
import { useUnreadCount } from "../hooks/useUnreadCount";
import { NotificationText } from "../view/notificationContent";
import { NotificationPanel } from "./NotificationPanel";
import { SecureAccountModal } from "./SecureAccountModal";
import styles from "./NotificationBell.module.css";

interface UnreadBadgeProps {
  readonly badge: UnreadBadgeState;
}

interface BellPanelProps {
  readonly onSecure: (notificationId: string) => void;
}

function joinClasses(...names: (string | false)[]): string {
  return names.filter(Boolean).join(" ");
}

function UnreadBadge({ badge }: UnreadBadgeProps) {
  const isRising = badge.direction === "up";
  const rollIn = isRising ? styles.digitIn : styles.digitInDown;
  const rollOut = isRising ? styles.digitOut : styles.digitOutDown;

  return (
    <span
      className={joinClasses(
        styles.badge,
        badge.isVisible && styles.badgeOn,
        badge.isVisible && badge.isAppearing && styles.badgePop,
        badge.arrival > 0 && styles.badgePulse,
      )}
      aria-hidden
    >
      <span className={styles.digits}>
        {badge.previousLabel !== null ? (
          <span
            key={`out-${badge.previousLabel}`}
            className={joinClasses(styles.digit, rollOut)}
          >
            {badge.previousLabel}
          </span>
        ) : null}
        <span
          key={badge.label}
          className={joinClasses(
            styles.digit,
            badge.previousLabel !== null && rollIn,
          )}
        >
          {badge.label}
        </span>
      </span>
    </span>
  );
}

function BellPanel({ onSecure }: BellPanelProps) {
  const { isOpen } = useDropdownContext();

  return (
    <DropdownPanel
      keepMounted
      role="dialog"
      aria-label={NotificationText.bell.label}
      placement="bottom-end"
      offset={14}
    >
      <NotificationPanel isOpen={isOpen} onSecure={onSecure} />
    </DropdownPanel>
  );
}

export function NotificationBell() {
  const { data: unreadCount } = useUnreadCount();
  const badge = useUnreadBadge(unreadCount);
  const [securingId, setSecuringId] = useState<string | null>(null);

  useRefreshOnNewNotifications();

  const label =
    badge.isVisible && unreadCount !== undefined
      ? NotificationText.bell.labelWithUnread(unreadCount)
      : NotificationText.bell.label;

  return (
    <>
      <Dropdown>
        <DropdownTrigger
          popup="dialog"
          className={joinClasses(
            styles.bell,
            badge.arrival > 0 && styles.ringing,
          )}
          aria-label={label}
        >
          <Icon
            key={`bell-${badge.arrival}`}
            name="notifications"
            className={styles.bellIcon}
          />
          <UnreadBadge key={`badge-${badge.arrival}`} badge={badge} />
        </DropdownTrigger>
        <BellPanel onSecure={setSecuringId} />
      </Dropdown>
      {securingId !== null ? (
        <SecureAccountModal
          notificationId={securingId}
          onClose={() => setSecuringId(null)}
        />
      ) : null}
    </>
  );
}
