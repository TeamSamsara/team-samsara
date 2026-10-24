// File : /team-samsara/apps/web/src/features/notification/ui/SignInAlertBanner.tsx
// Version : 1.1.0
// Latest commit: task/web-notifications
// Author : Gerrah
// Purpose : The amber banner under the navbar for an unread sign-in notice:
// which browser, from which address and when, with "That was me" and "Secure
// my account". It grows in when a notice appears, stays in the page while it
// collapses after the notice is answered, then removes itself. It owns the
// "secure your account" modal.

import { useState, type AnimationEvent } from "react";
import { Icon } from "@/shared/icons/Icon";
import { signInSummary } from "../data/notificationRegistry";
import type { NotificationItem } from "../types/notificationTypes";
import { formatBannerTime } from "../view/formatNotificationTime";
import { NotificationText } from "../view/notificationContent";
import { SecureAccountModal } from "./SecureAccountModal";
import { SignInAlertActions } from "./SignInAlertActions";
import styles from "./SignInAlertBanner.module.css";

interface SignInAlertBannerProps {
  readonly item: NotificationItem | null;
}

export function SignInAlertBanner({ item }: SignInAlertBannerProps) {
  const [isSecuring, setIsSecuring] = useState(false);
  const [shownItem, setShownItem] = useState(item);

  // Adjusted while rendering, the documented way to derive state from a prop:
  // the banner keeps the last notice so it can still show it while collapsing.
  if (item !== null && item !== shownItem) setShownItem(item);

  if (shownItem === null) return null;

  const isClosing = item === null;

  const handleAnimationEnd = (event: AnimationEvent<HTMLDivElement>) => {
    if (isClosing && event.target === event.currentTarget) {
      setShownItem(null);
    }
  };

  const detail = NotificationText.banner.detail(
    signInSummary(shownItem.data),
    formatBannerTime(shownItem.createdAt),
  );

  return (
    <>
      <div
        className={styles.collapse}
        data-closing={isClosing || undefined}
        inert={isClosing}
        onAnimationEnd={handleAnimationEnd}
      >
        <div className={styles.reveal}>
          <section
            className={styles.banner}
            aria-label={NotificationText.banner.title}
          >
            <Icon name="warning" className={styles.icon} />
            <div className={styles.text}>
              <p className={styles.title}>{NotificationText.banner.title}</p>
              <p className={styles.detail}>{detail}</p>
            </div>
            <div className={styles.actions}>
              <SignInAlertActions
                notificationId={shownItem.id}
                tone="banner"
                onSecure={() => setIsSecuring(true)}
              />
            </div>
          </section>
        </div>
      </div>
      {isSecuring ? (
        <SecureAccountModal
          notificationId={shownItem.id}
          onClose={() => setIsSecuring(false)}
        />
      ) : null}
    </>
  );
}
