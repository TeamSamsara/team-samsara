// File : /team-samsara/apps/web/src/features/notification/ui/NotificationPanel.tsx
// Version : 1.3.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : The content of the notification panel under the bell: the header
// with the "mark all as read" and "clear all read" icons, the list of notifications with "load more", and the
// empty and could-not-load messages. It stays in the page while closed and only
// loads the list while open; rows that arrive while it is open grow in.

import { NotificationKeys } from "../constants/notificationKeys";
import { useArrivingNotifications } from "../hooks/useArrivingNotifications";
import {
  useClearReadNotifications,
  useRemoveNotification,
} from "../hooks/useClearNotifications";
import {
  useMarkAllNotificationsRead,
  useMarkNotificationRead,
} from "../hooks/useMarkNotificationRead";
import { useNotificationList } from "../hooks/useNotificationList";
import { useUnreadCount } from "../hooks/useUnreadCount";
import type { NotificationItem } from "../types/notificationTypes";
import { NotificationText } from "../view/notificationContent";
import { NotificationIconButton } from "./NotificationIconButton";
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
  const remove = useRemoveNotification();
  const clearRead = useClearReadNotifications();

  const items = list.data?.pages.flatMap((page) => page.items) ?? [];
  const arrivingIds = useArrivingNotifications(
    items,
    isOpen,
    list.data !== undefined,
  );
  const hasRead = items.some((item) => item.readAt !== null);
  const hasFailed = list.isError && list.data === undefined;
  const isEmpty = !list.isPending && !hasFailed && items.length === 0;

  return (
    <div className={styles.panel} aria-busy={list.isPending || undefined}>
      <div className={styles.header}>
        <div className={styles.headerText}>
          <h2 className={styles.title}>{NotificationText.panel.title}</h2>
          <p className={styles.period}>{NotificationText.panel.period}</p>
        </div>
        <div className={styles.headerActions}>
          {unreadCount > 0 ? (
            <NotificationIconButton
              icon="checkAll"
              label={NotificationText.panel.markAllRead}
              disabled={markAll.isPending}
              onClick={() => markAll.mutate()}
            />
          ) : null}
          {hasRead ? (
            <NotificationIconButton
              icon="trash"
              label={NotificationText.panel.clearRead}
              tone="quiet"
              disabled={clearRead.isPending}
              onClick={() => clearRead.mutate()}
            />
          ) : null}
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
              onRemove={
                needsAnswer(item) ? undefined : (id) => remove.mutate(id)
              }
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
