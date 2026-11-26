// File : /team-samsara/apps/web/src/features/auth/view/RegisterPage.tsx
// Version : 1.0.0
// Latest commit: task/web-auth-screens
// Author : Gerrah
// Purpose : The Register screen: email and password, checked as each field is
// left. The account is created, then the person goes to the code screen to
// confirm their email.

import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "@/app/useAuth";
import { Button } from "@/shared/components/Button/Button";
import { PasswordField } from "@/shared/components/Fields/PasswordField/PasswordField";
import { TextField } from "@/shared/components/Fields/TextField/TextField";
import { routePaths } from "@/shared/constants/routePaths";
import { registerWithEmail } from "../service/registrationService";
import type { CodeScreenState } from "../types/codeScreenState";
import {
  validateEmail,
  validateNewPassword,
} from "../validation/authValidation";
import { AuthFormFrame } from "../ui/AuthFormFrame";
import { AuthSplitLayout } from "../ui/AuthSplitLayout";
import { RegisterPageText } from "./registerPageContent";

interface FieldErrors {
  readonly email: string | null;
  readonly password: string | null;
}

export function RegisterPage() {
  const navigate = useNavigate();
  const { refreshAccount } = useAuth();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [errors, setErrors] = useState<FieldErrors>({
    email: null,
    password: null,
  });
  const [formError, setFormError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // An error that is showing goes away as soon as the value is fixed.
  function changeEmail(value: string) {
    setEmail(value);
    if (errors.email) {
      setErrors((current) => ({ ...current, email: validateEmail(value) }));
    }
  }

  function changePassword(value: string) {
    setPassword(value);
    if (errors.password) {
      setErrors((current) => ({
        ...current,
        password: validateNewPassword(value),
      }));
    }
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (isSubmitting) {
      return;
    }

    const found: FieldErrors = {
      email: validateEmail(email),
      password: validateNewPassword(password),
    };
    setErrors(found);
    setFormError(null);

    if (found.email || found.password) {
      return;
    }

    setIsSubmitting(true);
    const attempt = await registerWithEmail(email, password);

    if (attempt.kind === "codeSent") {
      const state: CodeScreenState = {
        codeLength: attempt.codeLength,
        retryAfterSeconds: attempt.retryAfterSeconds,
      };

      await refreshAccount();
      void navigate(routePaths.registerVerify, { state });
      return;
    }

    setIsSubmitting(false);
    const message = RegisterPageText.failureMessage(
      attempt.reason,
      attempt.retryAfterSeconds,
    );

    if (attempt.reason === "invalidEmail") {
      setErrors((current) => ({ ...current, email: message }));
    } else if (attempt.reason === "weakPassword") {
      setErrors((current) => ({ ...current, password: message }));
    } else {
      setFormError(message);
    }
  }

  return (
    <AuthSplitLayout>
      <AuthFormFrame
        title={RegisterPageText.title}
        onSubmit={(event) => void submit(event)}
        error={formError}
        actions={
          <Button type="submit" fullWidth isLoading={isSubmitting}>
            {RegisterPageText.submit}
          </Button>
        }
        footer={
          <p>
            {RegisterPageText.loginPrompt}{" "}
            <Link to={routePaths.login}>{RegisterPageText.loginLink}</Link>
          </p>
        }
      >
        <TextField
          label={RegisterPageText.emailLabel}
          placeholder={RegisterPageText.emailLabel}
          type="email"
          name="email"
          autoComplete="email"
          value={email}
          error={errors.email}
          onChange={(event) => changeEmail(event.target.value)}
          onBlur={() =>
            setErrors((current) => ({
              ...current,
              email: validateEmail(email),
            }))
          }
        />
        <PasswordField
          label={RegisterPageText.passwordLabel}
          placeholder={RegisterPageText.passwordLabel}
          name="password"
          autoComplete="new-password"
          value={password}
          error={errors.password}
          onChange={(event) => changePassword(event.target.value)}
          onBlur={() =>
            setErrors((current) => ({
              ...current,
              password: validateNewPassword(password),
            }))
          }
        />
      </AuthFormFrame>
    </AuthSplitLayout>
  );
}
