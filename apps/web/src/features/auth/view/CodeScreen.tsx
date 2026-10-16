// File : /team-samsara/apps/web/src/features/auth/view/CodeScreen.tsx
// Version : 1.0.0
// Latest commit: task/web-auth-codes-reset
// Author : Gerrah
// Purpose : The inside of a code screen. While the code is being sent it says
// so, if that fails it offers to try again, and once the length is known it
// shows the code form. The register and log in code screens only add their
// own words and what happens when the code is accepted.

import type { FormEvent } from "react";
import { Button } from "@/shared/components/Button/Button";
import type { CodeAttempt, ResendAttempt } from "../types/codeAttempt";
import { AuthFormFrame } from "../ui/AuthFormFrame";
import { CodeVerificationForm } from "../ui/CodeVerificationForm";
import styles from "./CodeScreen.module.css";
import { CodeScreenText } from "./codeScreenContent";
import type { CodeSetup } from "./useCodeScreenSetup";

export interface CodeScreenProps {
  title: string;
  /** The sentence under the title, once the code length is known. */
  describe: (codeLength: number) => string;
  setup: CodeSetup;
  retry: () => void;
  onConfirm: (code: string) => Promise<CodeAttempt>;
  onResend: () => Promise<ResendAttempt>;
  secondaryAction?: { readonly label: string; readonly onClick: () => void };
}

function ignoreSubmit(event: FormEvent<HTMLFormElement>) {
  event.preventDefault();
}

export function CodeScreen({
  title,
  describe,
  setup,
  retry,
  onConfirm,
  onResend,
  secondaryAction,
}: CodeScreenProps) {
  if (setup.kind === "ready") {
    return (
      <CodeVerificationForm
        title={title}
        description={describe(setup.codeLength)}
        codeLength={setup.codeLength}
        initialRetryAfterSeconds={setup.retryAfterSeconds}
        onConfirm={onConfirm}
        onResend={onResend}
        secondaryAction={secondaryAction}
      />
    );
  }

  if (setup.kind === "loading") {
    return (
      <AuthFormFrame title={title} onSubmit={ignoreSubmit} actions={null}>
        <p className={styles.waiting} role="status">
          {CodeScreenText.sending}
        </p>
      </AuthFormFrame>
    );
  }

  return (
    <AuthFormFrame
      title={title}
      onSubmit={ignoreSubmit}
      error={setup.message}
      actions={
        <Button fullWidth onClick={retry}>
          {CodeScreenText.tryAgain}
        </Button>
      }
      footer={
        secondaryAction ? (
          <button
            type="button"
            className={styles.linkButton}
            onClick={secondaryAction.onClick}
          >
            {secondaryAction.label}
          </button>
        ) : null
      }
    >
      {null}
    </AuthFormFrame>
  );
}
