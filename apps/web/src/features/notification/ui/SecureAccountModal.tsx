// File : /team-samsara/apps/web/src/features/notifications/ui/SecureAccountModal.tsx
// Version : 1.0.0
// Latest commit: task/web-notifications
// Author : Gerrah
// Purpose : The "secure your account" modal opened from a sign-in notice. It
// marks the notice as read, ends every session of the account, then shows the
// same signed-out message as "sign out everywhere".

import { useState } from "react";
import { LogOutEverywhereText } from "@/features/account/view/logOutEverywhereContent";
import { endAllSessions } from "@/features/account/service/sessionsService";
import styles from "@/features/account/ui/SecurityStep.module.css";
import { SignedOutModal } from "@/features/account/ui/SignedOutModal";
import { CodeVerifyMessages } from "@/features/auth/view/codeVerifyContent";
import { Modal } from "@/shared/components/Modal/Modal";
import { markNotificationRead } from "../service/notificationService";
import { NotificationText } from "../view/notificationContent";

interface SecureAccountModalProps {
  readonly notificationId: string;
  readonly onClose: () => void;
}

export function SecureAccountModal({
  notificationId,
  onClose,
}: SecureAccountModalProps) {
  const [isWorking, setIsWorking] = useState(false);
  const [isDone, setIsDone] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function confirm() {
    if (isWorking) {
      return;
    }

    setIsWorking(true);
    setError(null);

    // Called through the service, not the hook: the hook would turn the notice
    // read in the cache at once, which removes the buttons that opened this
    // modal. It must finish before the sessions end, because the request needs
    // this device's session. Its outcome is ignored: the sign-out matters more.
    await markNotificationRead(notificationId);
    const result = await endAllSessions();

    if (result.kind === "done") {
      setIsDone(true);
      return;
    }

    setError(
      CodeVerifyMessages.failure(result.reason, result.retryAfterSeconds),
    );
    setIsWorking(false);
  }

  if (isDone) {
    return (
      <SignedOutModal
        title={LogOutEverywhereText.doneTitle}
        message={LogOutEverywhereText.doneMessage}
      />
    );
  }

  return (
    <Modal
      title={NotificationText.secure.title}
      dismissible={!isWorking}
      onClose={onClose}
    >
      <p className={styles.description}>
        {NotificationText.secure.description}
      </p>
      <p className={styles.note}>{NotificationText.secure.note}</p>
      {error ? (
        <p className={`${styles.note} ${styles.noteError}`} role="alert">
          {error}
        </p>
      ) : null}
      <div className={styles.actions}>
        <button
          type="button"
          className={styles.primaryDanger}
          disabled={isWorking}
          aria-busy={isWorking || undefined}
          onClick={() => void confirm()}
        >
          {NotificationText.secure.confirm}
        </button>
        <button
          type="button"
          className={styles.outline}
          disabled={isWorking}
          onClick={onClose}
        >
          {NotificationText.secure.cancel}
        </button>
      </div>
    </Modal>
  );
}
