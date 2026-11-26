// File : /team-samsara/apps/web/src/features/account/ui/SignedOutModal.tsx
// Version : 1.0.0
// Latest commit: task/web-account-security
// Author : Gerrah
// Purpose : The message shown after every session of the account was ended. Log in signs
// this device out and opens the login page; if nobody clicks, the person is taken to the
// home page after a few seconds, signed out. Sessions, delete account and the finished
// password and email changes all end here.

import { useCallback, useEffect, useRef } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "@/app/useAuth";
import { Modal } from "@/shared/components/Modal/Modal";
import { routePaths } from "@/shared/constants/routePaths";
import { useCountdown } from "@/shared/hooks/useCountdown";
import styles from "./SecurityStep.module.css";
import {
  SignedOutModalText,
  signedOutRedirectSeconds,
} from "./signedOutModalContent";

interface SignedOutModalProps {
  readonly title: string;
  /** Why the person was signed out and what to do next. */
  readonly message: string;
}

export function SignedOutModal({ title, message }: SignedOutModalProps) {
  const navigate = useNavigate();
  const { signOut } = useAuth();
  const countdown = useCountdown(signedOutRedirectSeconds);
  const hasLeft = useRef(false);

  // The login page only lets signed-out people in, so Log in signs out first. The home
  // page is open to everyone, so it is opened first and the sign-out follows.
  const leave = useCallback(
    async (destination: string, signOutFirst: boolean) => {
      if (hasLeft.current) {
        return;
      }

      hasLeft.current = true;

      if (signOutFirst) {
        await signOut();
        void navigate(destination, { replace: true });
        return;
      }

      void navigate(destination, { replace: true });
      await signOut();
    },
    [navigate, signOut],
  );

  useEffect(() => {
    if (countdown.secondsLeft === 0) {
      void leave(routePaths.home, false);
    }
  }, [countdown.secondsLeft, leave]);

  return (
    <Modal title={title} dismissible={false}>
      <p className={styles.description} role="status">
        {message}
      </p>
      <p className={styles.noteMuted}>
        {SignedOutModalText.redirectIn(countdown.secondsLeft)}
      </p>
      <div className={styles.actions}>
        <button
          type="button"
          className={styles.primary}
          onClick={() => void leave(routePaths.login, true)}
        >
          {SignedOutModalText.login}
        </button>
      </div>
    </Modal>
  );
}
