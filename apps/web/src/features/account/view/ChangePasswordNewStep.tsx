// File : /team-samsara/apps/web/src/features/account/view/ChangePasswordNewStep.tsx
// Version : 1.0.0
// Latest commit: task/web-account-security
// Author : Gerrah
// Purpose : The first step of "change password": the new password, typed twice. Sending
// it checks the rules and emails a code; nothing changes until the code is confirmed.

import { useState, type FormEvent } from "react";
import { CodeVerifyMessages } from "@/features/auth/view/codeVerifyContent";
import { validateNewPassword } from "@/features/auth/validation/authValidation";
import { PasswordField } from "@/shared/components/Fields/PasswordField/PasswordField";
import { Icon } from "@/shared/icons/Icon";
import { sendPasswordChangeCode } from "../service/passwordChangeService";
import { SecurityCodeFormText } from "../ui/securityCodeFormContent";
import styles from "../ui/SecurityStep.module.css";
import { ChangePasswordText } from "./changePasswordContent";

export interface PasswordCodeSent {
  readonly newPassword: string;
  readonly codeLength: number;
  readonly retryAfterSeconds: number;
}

interface ChangePasswordNewStepProps {
  /** Kept when the person comes back from the code step to change it. */
  readonly initialPassword: string;
  readonly onSent: (sent: PasswordCodeSent) => void;
  readonly onCancel: () => void;
}

export function ChangePasswordNewStep({
  initialPassword,
  onSent,
  onCancel,
}: ChangePasswordNewStepProps) {
  const [password, setPassword] = useState(initialPassword);
  const [confirm, setConfirm] = useState(initialPassword);
  const [passwordError, setPasswordError] = useState<string | null>(null);
  const [confirmError, setConfirmError] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const matches = confirm.length > 0 && confirm === password;

  function checkConfirm(value: string, against: string): string | null {
    if (value.length === 0) {
      return ChangePasswordText.confirmRequired;
    }

    return value === against ? null : ChangePasswordText.mismatch;
  }

  // An error that is showing goes away as soon as the value is fixed.
  function changePassword(value: string) {
    setPassword(value);
    if (passwordError) {
      setPasswordError(validateNewPassword(value));
    }
    if (confirmError) {
      setConfirmError(checkConfirm(confirm, value));
    }
  }

  function changeConfirm(value: string) {
    setConfirm(value);
    if (confirmError) {
      setConfirmError(checkConfirm(value, password));
    }
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (isSubmitting) {
      return;
    }

    const foundPassword = validateNewPassword(password);
    const foundConfirm = checkConfirm(confirm, password);
    setPasswordError(foundPassword);
    setConfirmError(foundConfirm);
    setFormError(null);

    if (foundPassword || foundConfirm) {
      return;
    }

    setIsSubmitting(true);
    const result = await sendPasswordChangeCode(password);

    switch (result.kind) {
      case "sent":
        onSent({
          newPassword: password,
          codeLength: result.codeLength,
          retryAfterSeconds: result.retryAfterSeconds,
        });
        return;
      case "weakPassword":
        setPasswordError(ChangePasswordText.weakPassword);
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
      <h2 className={styles.heading}>{ChangePasswordText.newHeading}</h2>
      <p className={styles.description}>{ChangePasswordText.newDescription}</p>

      <div className={styles.fields}>
        <PasswordField
          label={ChangePasswordText.newLabel}
          placeholder={ChangePasswordText.newLabel}
          name="new-password"
          autoComplete="new-password"
          value={password}
          error={passwordError}
          onChange={(event) => changePassword(event.target.value)}
          onBlur={() => setPasswordError(validateNewPassword(password))}
        />
        <PasswordField
          label={ChangePasswordText.confirmLabel}
          placeholder={ChangePasswordText.confirmLabel}
          name="confirm-new-password"
          autoComplete="new-password"
          value={confirm}
          error={confirmError}
          onChange={(event) => changeConfirm(event.target.value)}
          onBlur={() => setConfirmError(checkConfirm(confirm, password))}
        />
      </div>

      {matches ? (
        <p className={`${styles.note} ${styles.noteOk}`} role="status">
          <Icon name="check" className={styles.noteIcon} />
          {ChangePasswordText.match}
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
          disabled={isSubmitting}
          aria-busy={isSubmitting || undefined}
        >
          {ChangePasswordText.sendCode}
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
