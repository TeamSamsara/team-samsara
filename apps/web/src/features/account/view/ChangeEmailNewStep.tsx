// File : /team-samsara/apps/web/src/features/account/view/ChangeEmailNewStep.tsx
// Version : 1.2.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : The first step of "change email": the new address. Sending it emails one code
// to the current address and one to the new address. An address that another account
// already uses, or that the account already has, is refused before anything is sent.

import { useState, type FormEvent } from "react";
import { validateEmail } from "@/features/auth/validation/authValidation";
import { CodeVerifyMessages } from "@/features/auth/view/codeVerifyContent";
import { TextField } from "@/shared/components/Fields/TextField/TextField";
import { InlineMessage } from "@/shared/components/InlineMessage/InlineMessage";
import { sendEmailChangeCodes } from "../service/emailChangeService";
import { SecurityCodeFormText } from "../ui/securityCodeFormContent";
import styles from "../ui/SecurityStep.module.css";
import { ChangeEmailText } from "./changeEmailContent";

export interface EmailCodesSent {
  readonly newEmail: string;
  readonly codeLength: number;
  readonly retryAfterSeconds: number;
}

interface ChangeEmailNewStepProps {
  /** The address the account has now, shown in the description. */
  readonly currentEmail: string;
  /** Kept when the person comes back from a code step to change it. */
  readonly initialEmail: string;
  readonly onSent: (sent: EmailCodesSent) => void;
  readonly onCancel: () => void;
}

export function ChangeEmailNewStep({
  currentEmail,
  initialEmail,
  onSent,
  onCancel,
}: ChangeEmailNewStepProps) {
  const [email, setEmail] = useState(initialEmail);
  const [emailError, setEmailError] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  function check(value: string): string | null {
    const found = validateEmail(value);

    if (found) {
      return found;
    }

    return value.trim().toLowerCase() === currentEmail.toLowerCase()
      ? ChangeEmailText.sameAsCurrent
      : null;
  }

  // An error that is showing goes away as soon as the value is fixed.
  function changeEmail(value: string) {
    setEmail(value);
    if (emailError) {
      setEmailError(check(value));
    }
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (isSubmitting) {
      return;
    }

    const found = check(email);
    setEmailError(found);
    setFormError(null);

    if (found) {
      return;
    }

    setIsSubmitting(true);
    const address = email.trim();
    const result = await sendEmailChangeCodes(address);

    switch (result.kind) {
      case "sent":
        onSent({
          newEmail: address,
          codeLength: result.codeLength,
          retryAfterSeconds: result.retryAfterSeconds,
        });
        return;
      case "invalidEmail":
        setEmailError(ChangeEmailText.invalidEmail);
        break;
      case "emailTaken":
        setEmailError(ChangeEmailText.emailTaken);
        break;
      case "sameAsCurrent":
        setEmailError(ChangeEmailText.sameAsCurrent);
        break;
      case "failed":
        setFormError(
          CodeVerifyMessages.failure(result.reason, result.retryAfterSeconds),
        );
        break;
    }

    setIsSubmitting(false);
  }

  return (
    <form
      className={styles.step}
      onSubmit={(event) => void submit(event)}
      noValidate
    >
      <p className={styles.noteMuted}>{ChangeEmailText.newEyebrow}</p>
      <h2 className={styles.heading}>{ChangeEmailText.newHeading}</h2>
      <p className={styles.description}>
        {ChangeEmailText.newDescription(currentEmail)}
      </p>

      <div className={styles.fields}>
        <TextField
          label={ChangeEmailText.newLabel}
          placeholder={ChangeEmailText.newLabel}
          type="email"
          name="new-email"
          autoComplete="email"
          value={email}
          error={emailError}
          onChange={(event) => changeEmail(event.target.value)}
          onBlur={() => setEmailError(check(email))}
        />
      </div>

      <p className={styles.noteMuted}>{ChangeEmailText.newHint}</p>
      <InlineMessage message={formError} withIcon />

      <div className={styles.actions}>
        <button
          type="submit"
          className={styles.primary}
          disabled={isSubmitting}
          aria-busy={isSubmitting || undefined}
        >
          {ChangeEmailText.sendCodes}
        </button>
        <button
          type="button"
          className={styles.outline}
          disabled={isSubmitting}
          onClick={onCancel}
        >
          {SecurityCodeFormText.cancel}
        </button>
      </div>
    </form>
  );
}
