// File : /team-samsara/apps/web/src/features/auth/view/LoginPage.tsx
// Version : 1.1.0
// Latest commit: task/web-auth-onboarding-navbar
// Author : Gerrah
// Purpose : The Log in screen: email and password, checked as each field is
// left. A verified member lands on the welcome step if they have not finished
// or skipped it yet, on the home page otherwise; anyone who still needs a code
// is sent to the right code screen.

import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "@/app/useAuth";
import { pathAfterLogin } from "@/features/account/service/landingService";
import { Button } from "@/shared/components/Button/Button";
import { PasswordField } from "@/shared/components/Fields/PasswordField/PasswordField";
import { TextField } from "@/shared/components/Fields/TextField/TextField";
import { routePaths } from "@/shared/constants/routePaths";
import { logInWithEmail } from "../service/loginService";
import type { CodeScreenState } from "../types/codeScreenState";
import { AuthCenteredLayout } from "../ui/AuthCenteredLayout";
import { AuthFormFrame } from "../ui/AuthFormFrame";
import {
  validateEmail,
  validateExistingPassword,
} from "../validation/authValidation";
import { LoginPageText } from "./loginPageContent";

interface FieldErrors {
  readonly email: string | null;
  readonly password: string | null;
}

export function LoginPage() {
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
        password: validateExistingPassword(value),
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
      password: validateExistingPassword(password),
    };
    setErrors(found);
    setFormError(null);

    if (found.email || found.password) {
      return;
    }

    setIsSubmitting(true);
    const attempt = await logInWithEmail(email, password, refreshAccount);

    switch (attempt.kind) {
      case "member":
        void navigate(await pathAfterLogin(), { replace: true });
        return;
      case "needsCode": {
        const state: CodeScreenState = {
          codeLength: attempt.codeLength,
          retryAfterSeconds: attempt.retryAfterSeconds,
        };

        await refreshAccount();
        void navigate(routePaths.loginVerify, { state });
        return;
      }
      case "registrationIncomplete":
        await refreshAccount();
        void navigate(routePaths.registerVerify);
        return;
      case "failed":
        setIsSubmitting(false);
        setFormError(
          LoginPageText.failureMessage(
            attempt.reason,
            attempt.retryAfterSeconds,
          ),
        );
        return;
    }
  }

  return (
    <AuthCenteredLayout>
      <AuthFormFrame
        title={LoginPageText.title}
        onSubmit={(event) => void submit(event)}
        error={formError}
        actions={
          <Button type="submit" fullWidth isLoading={isSubmitting}>
            {LoginPageText.submit}
          </Button>
        }
        footer={
          <>
            <Link to={routePaths.forgotPassword}>
              {LoginPageText.forgotPasswordLink}
            </Link>
            <p>
              {LoginPageText.registerPrompt}{" "}
              <Link to={routePaths.register}>{LoginPageText.registerLink}</Link>
            </p>
          </>
        }
      >
        <TextField
          label={LoginPageText.emailLabel}
          placeholder={LoginPageText.emailLabel}
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
          label={LoginPageText.passwordLabel}
          placeholder={LoginPageText.passwordLabel}
          name="password"
          autoComplete="current-password"
          value={password}
          error={errors.password}
          onChange={(event) => changePassword(event.target.value)}
          onBlur={() =>
            setErrors((current) => ({
              ...current,
              password: validateExistingPassword(password),
            }))
          }
        />
      </AuthFormFrame>
    </AuthCenteredLayout>
  );
}
