// File : /team-samsara/apps/web/src/features/notification/ui/NotificationPanel.tsx
// Version : 1.1.0
// Latest commit: task/web-notifications
// Author : Gerrah
// Purpose : The content of the notification panel under the bell: the header
// with "mark all as read", the list of notifications with "load more", and the
// empty and could-not-load messages. It stays in the page while closed and only
// loads the list while open; rows that arrive while it is open grow in.

import { NotificationKeys } from "../constants/notificationKeys";
import { useArrivingNotifications } from "../hooks/useArrivingNotifications";
import {
  useMarkAllNotificationsRead,
  useMarkNotificationRead,
} from "../hooks/useMarkNotificationRead";
import { useNotificationList } from "../hooks/useNotificationList";
import { useUnreadCount } from "../hooks/useUnreadCount";
import type { NotificationItem } from "../types/notificationTypes";
import { NotificationText } from "../view/notificationContent";
import { NotificationRow } from "./NotificationRow";
import { SignInAlertActions } from "./SignInAlertActions";
import styles from "./NotificationPanel.module.css";

interface NotificationPanelProps {
  readonly isOpen: boolean;
  readonly onSecure: (notificationId: string) => void;
}

function needsAnswer(item: NotificationItem): boolean {
  return item.key === NotificationKeys.newLogin && item.readAt === null;
}

function EmptyState() {
  return (
    <div className={styles.state}>
      <p className={styles.stateTitle}>{NotificationText.panel.emptyTitle}</p>
      <p className={styles.stateMessage}>
        {NotificationText.panel.emptyMessage}
      </p>
    </div>
  );
}

function ErrorState({ onRetry }: { readonly onRetry: () => void }) {
  return (
    <div className={styles.state} role="alert">
      <p className={styles.stateTitle}>{NotificationText.panel.errorTitle}</p>
      <p className={styles.stateMessage}>
        {NotificationText.panel.errorMessage}
      </p>
      <button type="button" className={styles.retry} onClick={onRetry}>
        {NotificationText.panel.retry}
      </button>
    </div>
  );
}

export function NotificationPanel({
  isOpen,
  onSecure,
}: NotificationPanelProps) {
  const list = useNotificationList(isOpen);
  const { data: unreadCount = 0 } = useUnreadCount();
  const markRead = useMarkNotificationRead();
  const markAll = useMarkAllNotificationsRead();

  const items = list.data?.pages.flatMap((page) => page.items) ?? [];
  const arrivingIds = useArrivingNotifications(
    items,
    isOpen,
    list.data !== undefined,
  );
  const hasFailed = list.isError && list.data === undefined;
  const isEmpty = !list.isPending && !hasFailed && items.length === 0;

  return (
    <div className={styles.panel} aria-busy={list.isPending || undefined}>
      <div className={styles.header}>
        <h2 className={styles.title}>{NotificationText.panel.title}</h2>
        <div className={styles.headerAside}>
          {unreadCount > 0 ? (
            <button
              type="button"
              className={styles.markAll}
              disabled={markAll.isPending}
              onClick={() => markAll.mutate()}
            >
              {NotificationText.panel.markAllRead}
            </button>
          ) : null}
          <p className={styles.period}>{NotificationText.panel.period}</p>
        </div>
      </div>

      {hasFailed ? <ErrorState onRetry={() => void list.refetch()} /> : null}
      {isEmpty ? <EmptyState /> : null}

      {items.length > 0 ? (
        <ul className={styles.list}>
          {items.map((item) => (
            <NotificationRow
              key={item.id}
              item={item}
              isArriving={arrivingIds.has(item.id)}
              onMarkRead={(id) => markRead.mutate(id)}
            >
              {needsAnswer(item) ? (
                <SignInAlertActions
                  notificationId={item.id}
                  tone="panel"
                  onSecure={onSecure}
                />
              ) : null}
            </NotificationRow>
          ))}
        </ul>
      ) : null}

      {list.hasNextPage ? (
        <button
          type="button"
          className={styles.more}
          disabled={list.isFetchingNextPage}
          onClick={() => void list.fetchNextPage()}
        >
          {list.isFetchingNextPage
            ? NotificationText.panel.loadingMore
            : NotificationText.panel.loadMore}
        </button>
      ) : null}
    </div>
  );
}
