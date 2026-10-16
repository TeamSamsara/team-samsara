// File : /team-samsara/apps/web/src/features/auth/view/ForgotEmailStep.tsx
// Version : 1.0.0
// Latest commit: task/web-auth-codes-reset
// Author : Gerrah
// Purpose : The first step of "forgot password": the email address of the
// account. The answer is the same whether or not the account exists, so this
// step never says which one it was.

import { useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { Button } from "@/shared/components/Button/Button";
import { TextField } from "@/shared/components/Fields/TextField/TextField";
import { routePaths } from "@/shared/constants/routePaths";
import { requestResetCode } from "../service/passwordResetService";
import { AuthFormFrame } from "../ui/AuthFormFrame";
import { validateEmail } from "../validation/authValidation";
import { CodeVerifyMessages } from "./codeVerifyContent";
import { ForgotEmailStepText } from "./forgotEmailStepContent";
import styles from "./ForgotPassword.module.css";

export interface ResetCodeSent {
  readonly email: string;
  readonly codeLength: number;
  readonly retryAfterSeconds: number;
}

interface ForgotEmailStepProps {
  /** Kept when the person comes back from the code step to change it. */
  readonly initialEmail: string;
  readonly onSent: (sent: ResetCodeSent) => void;
}

export function ForgotEmailStep({
  initialEmail,
  onSent,
}: ForgotEmailStepProps) {
  const [email, setEmail] = useState(initialEmail);
  const [emailError, setEmailError] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // An error that is showing goes away as soon as the value is fixed.
  function changeEmail(value: string) {
    setEmail(value);
    if (emailError) {
      setEmailError(validateEmail(value));
    }
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (isSubmitting) {
      return;
    }

    const found = validateEmail(email);
    setEmailError(found);
    setFormError(null);

    if (found) {
      return;
    }

    setIsSubmitting(true);
    const address = email.trim();
    const result = await requestResetCode(address);

    if (result.kind === "sent") {
      onSent({
        email: address,
        codeLength: result.codeLength,
        retryAfterSeconds: result.retryAfterSeconds,
      });
      return;
    }

    setIsSubmitting(false);
    setFormError(
      CodeVerifyMessages.failure(result.reason, result.retryAfterSeconds),
    );
  }

  return (
    <AuthFormFrame
      title={ForgotEmailStepText.title}
      onSubmit={(event) => void submit(event)}
      error={formError}
      actions={
        <Button type="submit" fullWidth isLoading={isSubmitting}>
          {ForgotEmailStepText.submit}
        </Button>
      }
      footer={
        <Link to={routePaths.login}>{ForgotEmailStepText.backToLogin}</Link>
      }
    >
      <p className={styles.description}>{ForgotEmailStepText.description}</p>
      <TextField
        label={ForgotEmailStepText.emailLabel}
        placeholder={ForgotEmailStepText.emailLabel}
        type="email"
        name="email"
        autoComplete="email"
        value={email}
        error={emailError}
        onChange={(event) => changeEmail(event.target.value)}
        onBlur={() => setEmailError(validateEmail(email))}
      />
    </AuthFormFrame>
  );
}
