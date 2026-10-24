// File : /team-samsara/apps/web/src/features/notifications/ui/SignInAlertActions.tsx
// Version : 1.0.0
// Latest commit: task/web-notifications
// Author : Gerrah
// Purpose : The two buttons of a sign-in notice, shared by the banner and the
// panel row. "That was me" marks the notice read; "Secure my account" asks the
// owner of the screen to open the confirmation, so the confirmation outlives
// the panel that closes when focus leaves it.

import { useMarkNotificationRead } from "../hooks/useMarkNotificationRead";
import { NotificationText } from "../view/notificationContent";
import styles from "./SignInAlertActions.module.css";

interface SignInAlertActionsProps {
  readonly notificationId: string;
  readonly tone: "panel" | "banner";
  readonly onSecure: (notificationId: string) => void;
}

export function SignInAlertActions({
  notificationId,
  tone,
  onSecure,
}: SignInAlertActionsProps) {
  const markRead = useMarkNotificationRead();

  return (
    <>
      <button
        type="button"
        className={`${styles.button} ${styles.outline}`}
        data-tone={tone}
        onClick={() => markRead.mutate(notificationId)}
      >
        {NotificationText.banner.thatWasMe}
      </button>
      <button
        type="button"
        className={`${styles.button} ${styles.secure}`}
        data-tone={tone}
        onClick={() => onSecure(notificationId)}
      >
        {NotificationText.banner.secureMyAccount}
      </button>
    </>
  );
}
