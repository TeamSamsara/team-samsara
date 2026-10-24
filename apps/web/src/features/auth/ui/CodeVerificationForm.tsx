// File : /team-samsara/apps/web/src/features/auth/ui/CodeVerificationForm.tsx
// Version : 1.2.0
// Latest commit: task/web-toast-messages
// Author : Gerrah
// Purpose : The body of every screen that asks for an emailed code: the boxes,
// the button, and a resend link with its countdown. The screen decides what a
// code is for; this form only collects it and shows how it went. A new code
// being sent shows as a toast.

import { useState, type FormEvent } from "react";
import { Button } from "@/shared/components/Button/Button";
import { CodeInput } from "@/shared/components/Fields/CodeInput/CodeInput";
import { useToast } from "@/shared/components/Toast/useToast";
import { useCountdown } from "@/shared/hooks/useCountdown";
import type { CodeAttempt, ResendAttempt } from "../types/codeAttempt";
import styles from "./CodeVerificationForm.module.css";
import { AuthFormFrame } from "./AuthFormFrame";
import { CodeVerificationText } from "./codeVerificationFormContent";

export interface CodeVerificationFormProps {
  title: string;
  /** The sentence under the title: what was sent and when it expires. */
  description: string;
  /** How many digits the code has; the API says so when it sends the code. */
  codeLength: number;
  /** Seconds before a new code can be asked for, as the API last said. */
  initialRetryAfterSeconds: number;
  onConfirm: (code: string) => Promise<CodeAttempt>;
  onResend: () => Promise<ResendAttempt>;
  /** A second link under the resend link, such as "use another account". */
  secondaryAction?: { readonly label: string; readonly onClick: () => void };
}

export function CodeVerificationForm({
  title,
  description,
  codeLength,
  initialRetryAfterSeconds,
  onConfirm,
  onResend,
  secondaryAction,
}: CodeVerificationFormProps) {
  const { show } = useToast();
  const [code, setCode] = useState("");
  const [codeError, setCodeError] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [isConfirming, setIsConfirming] = useState(false);
  const [isResending, setIsResending] = useState(false);
  // A new round starts the boxes again, with the cursor in the first one.
  const [round, setRound] = useState(0);
  const countdown = useCountdown(initialRetryAfterSeconds);

  const isBusy = isConfirming || isResending;

  async function confirm(value: string) {
    if (isConfirming) {
      return;
    }
    if (value.length !== codeLength) {
      setCodeError(CodeVerificationText.incomplete(codeLength));
      return;
    }

    setCodeError(null);
    setFormError(null);
    setIsConfirming(true);

    const attempt = await onConfirm(value);

    switch (attempt.kind) {
      case "accepted":
        // The screen changes next, so the form stays busy until it does.
        return;
      case "wrongCode":
        setCode("");
        setCodeError(attempt.message);
        setRound((current) => current + 1);
        break;
      case "failed":
        setFormError(attempt.message);
        break;
    }

    setIsConfirming(false);
  }

  async function resend() {
    if (isBusy || countdown.secondsLeft > 0) {
      return;
    }

    setCodeError(null);
    setFormError(null);
    setIsResending(true);

    const attempt = await onResend();

    if (attempt.kind === "waiting") {
      countdown.start(attempt.retryAfterSeconds);
      setCode("");
      show({ tone: "success", title: CodeVerificationText.resent });
      setRound((current) => current + 1);
    } else {
      setFormError(attempt.message);
    }

    setIsResending(false);
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    void confirm(code);
  }

  return (
    <AuthFormFrame
      title={title}
      onSubmit={submit}
      error={formError}
      actions={
        <Button type="submit" fullWidth isLoading={isConfirming}>
          {CodeVerificationText.submit}
        </Button>
      }
      footer={
        <>
          <button
            type="button"
            className={styles.linkButton}
            disabled={isBusy || countdown.secondsLeft > 0}
            onClick={() => void resend()}
          >
            {countdown.secondsLeft > 0
              ? CodeVerificationText.resendIn(countdown.secondsLeft)
              : CodeVerificationText.resend}
          </button>
          {secondaryAction ? (
            <button
              type="button"
              className={styles.linkButton}
              disabled={isBusy}
              onClick={secondaryAction.onClick}
            >
              {secondaryAction.label}
            </button>
          ) : null}
        </>
      }
    >
      <p className={styles.description}>{description}</p>
      <CodeInput
        key={round}
        length={codeLength}
        value={code}
        onChange={setCode}
        onComplete={(value) => void confirm(value)}
        label={CodeVerificationText.codeLabel}
        error={codeError}
        disabled={isBusy}
        autoFocus
      />
    </AuthFormFrame>
  );
}
