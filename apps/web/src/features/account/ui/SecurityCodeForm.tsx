// File : /team-samsara/apps/web/src/features/account/ui/SecurityCodeForm.tsx
// Version : 1.2.0
// Latest commit: task/web-account-security
// Author : Gerrah
// Purpose : The code step shared by the account security pages: the heading, the code
// boxes, the confirm and cancel buttons, and a resend link with its countdown. The page
// decides what the code is for; this form only collects it and shows how it went.

import { useState, type FormEvent } from "react";
import type {
  CodeAttempt,
  ResendAttempt,
} from "@/features/auth/types/codeAttempt";
import { CodeVerificationText } from "@/features/auth/ui/codeVerificationFormContent";
import { CodeInput } from "@/shared/components/Fields/CodeInput/CodeInput";
import { useCountdown } from "@/shared/hooks/useCountdown";
import { Icon } from "@/shared/icons/Icon";
import styles from "./SecurityStep.module.css";
import { SecurityCodeFormText } from "./securityCodeFormContent";

interface SecurityCodeFormProps {
  /** Left out when the form sits in a modal whose title already says it. */
  readonly heading?: string;
  /** A small line above the heading, such as which step of the flow this is. */
  readonly eyebrow?: string;
  /** A line of good news under the heading, such as a step that is already done. */
  readonly confirmedNote?: string;
  /** What was sent and when it expires. */
  readonly description: string;
  /** How many digits the code has; the API says so when it sends the code. */
  readonly codeLength: number;
  /** Seconds before a new code can be asked for, as the API last said. */
  readonly initialRetryAfterSeconds: number;
  /** The label of the button that sends the code. */
  readonly submitLabel: string;
  /** Shown under a wrong code, such as how many tries are left. */
  readonly wrongCodeNote?: string;
  /** Uses the error color for the heading and the button. */
  readonly danger?: boolean;
  readonly onConfirm: (code: string) => Promise<CodeAttempt>;
  readonly onResend: () => Promise<ResendAttempt>;
  readonly onCancel: () => void;
}

export function SecurityCodeForm({
  heading,
  eyebrow,
  confirmedNote,
  description,
  codeLength,
  initialRetryAfterSeconds,
  submitLabel,
  wrongCodeNote,
  danger = false,
  onConfirm,
  onResend,
  onCancel,
}: SecurityCodeFormProps) {
  const [code, setCode] = useState("");
  const [codeError, setCodeError] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
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
    setNotice(null);
    setIsConfirming(true);

    const attempt = await onConfirm(value);

    switch (attempt.kind) {
      case "accepted":
        // The page changes next, so the form stays busy until it does.
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
    setNotice(null);
    setIsResending(true);

    const attempt = await onResend();

    if (attempt.kind === "waiting") {
      countdown.start(attempt.retryAfterSeconds);
      setCode("");
      setNotice(CodeVerificationText.resent);
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
    <form
      className={`${styles.step} ${danger ? styles.danger : ""}`}
      onSubmit={submit}
      noValidate
    >
      {eyebrow ? <p className={styles.noteMuted}>{eyebrow}</p> : null}
      {heading ? <h2 className={styles.heading}>{heading}</h2> : null}
      {confirmedNote ? (
        <p className={`${styles.note} ${styles.noteOk}`}>
          <Icon name="check" className={styles.noteIcon} />
          {confirmedNote}
        </p>
      ) : null}
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

      {codeError && wrongCodeNote ? (
        <p className={styles.noteMuted}>{wrongCodeNote}</p>
      ) : null}
      {notice ? (
        <p className={`${styles.note} ${styles.noteOk}`} role="status">
          {notice}
        </p>
      ) : null}
      {formError ? (
        <p className={`${styles.note} ${styles.noteError}`} role="alert">
          {formError}
        </p>
      ) : null}

      <div className={styles.actions}>
        <button
          type="submit"
          className={styles.primary}
          disabled={isBusy}
          aria-busy={isConfirming || undefined}
        >
          {submitLabel}
        </button>
        <button
          type="button"
          className={styles.outline}
          disabled={isConfirming}
          onClick={onCancel}
        >
          {SecurityCodeFormText.cancel}
        </button>
      </div>

      <p className={styles.resend}>
        {SecurityCodeFormText.didNotGetIt}{" "}
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
      </p>
    </form>
  );
}
