// File : /team-samsara/apps/web/src/features/account/view/ChangePasswordPage.tsx
// Version : 1.0.0
// Latest commit: task/web-account-security
// Author : Gerrah
// Purpose : The change-password page at /account/security/password. It runs three steps in
// one place: the new password, the emailed code and the confirmation. The new password
// lives only in this page's memory until the change is made or the person leaves.

import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "@/app/useAuth";
import type {
  CodeAttempt,
  ResendAttempt,
} from "@/features/auth/types/codeAttempt";
import {
  attemptFromProblem,
  resendAttemptFrom,
} from "@/features/auth/view/codeVerifyResults";
import { routePaths } from "@/shared/constants/routePaths";
import {
  confirmPasswordChange,
  sendPasswordChangeCode,
} from "../service/passwordChangeService";
import { SecurityCodeForm } from "../ui/SecurityCodeForm";
import { SecurityPageShell } from "../ui/SecurityPageShell";
import { ChangePasswordDoneStep } from "./ChangePasswordDoneStep";
import {
  ChangePasswordNewStep,
  type PasswordCodeSent,
} from "./ChangePasswordNewStep";
import { ChangePasswordText } from "./changePasswordContent";

type Step =
  | { readonly kind: "new"; readonly password: string }
  | { readonly kind: "code"; readonly sent: PasswordCodeSent }
  | { readonly kind: "done" };

export function ChangePasswordPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [step, setStep] = useState<Step>({ kind: "new", password: "" });

  function leave() {
    void navigate(routePaths.accountSecurity);
  }

  async function confirm(
    sent: PasswordCodeSent,
    code: string,
  ): Promise<CodeAttempt> {
    const result = await confirmPasswordChange(sent.newPassword, code);

    if (result.kind === "done") {
      setStep({ kind: "done" });
      return { kind: "accepted" };
    }

    if (result.kind === "weakPassword") {
      return { kind: "failed", message: ChangePasswordText.weakPassword };
    }

    return attemptFromProblem(result);
  }

  async function resend(sent: PasswordCodeSent): Promise<ResendAttempt> {
    const result = await sendPasswordChangeCode(sent.newPassword);

    if (result.kind === "weakPassword") {
      return { kind: "failed", message: ChangePasswordText.weakPassword };
    }

    return resendAttemptFrom(result);
  }

  function renderStep() {
    switch (step.kind) {
      case "new":
        return (
          <ChangePasswordNewStep
            initialPassword={step.password}
            onSent={(sent) => setStep({ kind: "code", sent })}
            onCancel={leave}
          />
        );
      case "code":
        return (
          <SecurityCodeForm
            heading={ChangePasswordText.codeHeading}
            description={ChangePasswordText.codeDescription(
              user?.email ?? "",
              step.sent.codeLength,
            )}
            codeLength={step.sent.codeLength}
            initialRetryAfterSeconds={step.sent.retryAfterSeconds}
            submitLabel={ChangePasswordText.codeSubmit}
            wrongCodeNote={ChangePasswordText.wrongCodeNote}
            onConfirm={(code) => confirm(step.sent, code)}
            onResend={() => resend(step.sent)}
            onCancel={leave}
          />
        );
      case "done":
        return <ChangePasswordDoneStep />;
    }
  }

  return (
    <SecurityPageShell title={ChangePasswordText.title}>
      {renderStep()}
    </SecurityPageShell>
  );
}
