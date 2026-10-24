// File : /team-samsara/apps/web/src/features/notification/ui/NotificationRow.tsx
// Version : 1.1.0
// Latest commit: task/web-notifications
// Author : Gerrah
// Purpose : One notification in the panel: icon, title, detail and time. An
// unread one also shows the unread square and, unless it has its own action
// buttons, a "mark as read" link. A row that arrives while the panel is open
// is flagged so it can grow in. Extra actions, such as the sign-in buttons,
// come in as children.

import type { ReactNode } from "react";
import { Icon } from "@/shared/icons/Icon";
import { presentNotification } from "../data/notificationRegistry";
import type { NotificationItem } from "../types/notificationTypes";
import { formatRelativeTime } from "../view/formatNotificationTime";
import { NotificationText } from "../view/notificationContent";
import styles from "./NotificationRow.module.css";

interface NotificationRowProps {
  readonly item: NotificationItem;
  readonly onMarkRead: (id: string) => void;
  readonly isArriving?: boolean;
  readonly children?: ReactNode;
}

export function NotificationRow({
  item,
  onMarkRead,
  isArriving = false,
  children,
}: NotificationRowProps) {
  const { icon, title, detail } = presentNotification(item);
  const isUnread = item.readAt === null;
  const hasActions = Boolean(children);

  return (
    <li
      className={styles.row}
      data-unread={isUnread}
      data-arriving={isArriving || undefined}
    >
      <div className={styles.content}>
        <span className={styles.iconWrap}>
          <Icon name={icon} className={styles.icon} />
        </span>
        <div className={styles.body}>
          <p className={styles.title}>{title}</p>
          <p className={styles.detail}>{detail}</p>
          {hasActions ? <div className={styles.actions}>{children}</div> : null}
          <p className={styles.meta}>
            <time dateTime={item.createdAt}>
              {formatRelativeTime(item.createdAt)}
            </time>
            {isUnread && !hasActions ? (
              <button
                type="button"
                className={styles.markRead}
                onClick={() => onMarkRead(item.id)}
              >
                {NotificationText.panel.markRead}
              </button>
            ) : null}
          </p>
        </div>
      </div>
      <span className={styles.unreadMark} aria-hidden />
    </li>
  );
}
