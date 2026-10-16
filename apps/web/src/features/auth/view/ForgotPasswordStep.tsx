// File : /team-samsara/apps/web/src/features/auth/view/ForgotPasswordStep.tsx
// Version : 1.0.0
// Latest commit: task/web-auth-codes-reset
// Author : Gerrah
// Purpose : The third step of "forgot password": the new password. The token
// from the code step is used once, here. A password the rules refuse can be
// fixed on this step; an expired token means starting over.

import { useState, type FormEvent } from "react";
import { Button } from "@/shared/components/Button/Button";
import { PasswordField } from "@/shared/components/Fields/PasswordField/PasswordField";
import { resetPassword } from "../service/passwordResetService";
import { AuthFormFrame } from "../ui/AuthFormFrame";
import { validateNewPassword } from "../validation/authValidation";
import { CodeVerifyMessages } from "./codeVerifyContent";
import { ForgotPasswordStepText } from "./forgotPasswordStepContent";
import styles from "./ForgotPassword.module.css";

interface ForgotPasswordStepProps {
  readonly resetToken: string;
  readonly onDone: () => void;
  /** Back to the first step, forgetting the token. */
  readonly onStartOver: () => void;
}

export function ForgotPasswordStep({
  resetToken,
  onDone,
  onStartOver,
}: ForgotPasswordStepProps) {
  const [password, setPassword] = useState("");
  const [passwordError, setPasswordError] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // An error that is showing goes away as soon as the value is fixed.
  function changePassword(value: string) {
    setPassword(value);
    if (passwordError) {
      setPasswordError(validateNewPassword(value));
    }
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (isSubmitting) {
      return;
    }

    const found = validateNewPassword(password);
    setPasswordError(found);
    setFormError(null);

    if (found) {
      return;
    }

    setIsSubmitting(true);
    const result = await resetPassword(resetToken, password);

    switch (result.kind) {
      case "done":
        onDone();
        return;
      case "weakPassword":
        setPasswordError(ForgotPasswordStepText.weakPassword);
        break;
      case "tokenExpired":
        setFormError(ForgotPasswordStepText.tokenExpired);
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
    <AuthFormFrame
      title={ForgotPasswordStepText.title}
      onSubmit={(event) => void submit(event)}
      error={formError}
      actions={
        <Button type="submit" fullWidth isLoading={isSubmitting}>
          {ForgotPasswordStepText.submit}
        </Button>
      }
      footer={
        <button
          type="button"
          className={styles.linkButton}
          disabled={isSubmitting}
          onClick={onStartOver}
        >
          {ForgotPasswordStepText.startOver}
        </button>
      }
    >
      <p className={styles.description}>{ForgotPasswordStepText.description}</p>
      <PasswordField
        label={ForgotPasswordStepText.passwordLabel}
        placeholder={ForgotPasswordStepText.passwordLabel}
        name="new-password"
        autoComplete="new-password"
        value={password}
        error={passwordError}
        onChange={(event) => changePassword(event.target.value)}
        onBlur={() => setPasswordError(validateNewPassword(password))}
      />
    </AuthFormFrame>
  );
}
