// File : /team-samsara/apps/web/src/features/auth/view/ForgotDoneStep.tsx
// Version : 1.0.0
// Latest commit: task/web-auth-codes-reset
// Author : Gerrah
// Purpose : The last step of "forgot password": the password was changed.
// Changing it signed the account out everywhere, so the way on is Log in.

import type { FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { Button } from "@/shared/components/Button/Button";
import { routePaths } from "@/shared/constants/routePaths";
import { AuthFormFrame } from "../ui/AuthFormFrame";
import { ForgotDoneStepText } from "./forgotPasswordStepContent";
import styles from "./ForgotPassword.module.css";

export function ForgotDoneStep() {
  const navigate = useNavigate();

  function goToLogin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    void navigate(routePaths.login, { replace: true });
  }

  return (
    <AuthFormFrame
      title={ForgotDoneStepText.title}
      onSubmit={goToLogin}
      actions={
        <Button type="submit" fullWidth>
          {ForgotDoneStepText.login}
        </Button>
      }
    >
      <p className={styles.description}>{ForgotDoneStepText.description}</p>
    </AuthFormFrame>
  );
}
