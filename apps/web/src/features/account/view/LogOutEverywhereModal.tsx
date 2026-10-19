// File : /team-samsara/apps/web/src/features/account/view/LogOutEverywhereModal.tsx
// Version : 1.0.0
// Latest commit: task/web-account-security
// Author : Gerrah
// Purpose : The "sign out everywhere" modal opened from the security overview. It asks
// first; once every session is ended it turns into the signed-out message.

import { useState } from "react";
import { CodeVerifyMessages } from "@/features/auth/view/codeVerifyContent";
import { Modal } from "@/shared/components/Modal/Modal";
import { endAllSessions } from "../service/sessionsService";
import { SignedOutModal } from "../ui/SignedOutModal";
import styles from "../ui/SecurityStep.module.css";
import { LogOutEverywhereText } from "./logOutEverywhereContent";

interface LogOutEverywhereModalProps {
  readonly onClose: () => void;
}

export function LogOutEverywhereModal({ onClose }: LogOutEverywhereModalProps) {
  const [isWorking, setIsWorking] = useState(false);
  const [isDone, setIsDone] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function confirm() {
    if (isWorking) {
      return;
    }

    setIsWorking(true);
    setError(null);

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
      title={LogOutEverywhereText.title}
      dismissible={!isWorking}
      onClose={onClose}
    >
      <p className={styles.description}>{LogOutEverywhereText.description}</p>
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
          {LogOutEverywhereText.confirm}
        </button>
        <button
          type="button"
          className={styles.outline}
          disabled={isWorking}
          onClick={onClose}
        >
          {LogOutEverywhereText.cancel}
        </button>
      </div>
    </Modal>
  );
}
