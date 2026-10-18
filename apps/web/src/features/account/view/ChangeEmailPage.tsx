// File : /team-samsara/apps/web/src/features/account/view/ChangeEmailPage.tsx
// Version : 1.0.0
// Latest commit: task/web-account-security
// Author : Gerrah
// Purpose : The change-email page at /account/security/email. It runs four steps in one
// place: the new address, the code from the current address, the code from the new
// address and the confirmation. Asking for new codes starts again at the current address,
// because a new request replaces the old one.

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
  confirmCurrentAddressCode,
  confirmNewAddressCode,
  sendEmailChangeCodes,
} from "../service/emailChangeService";
import { SecurityCodeForm } from "../ui/SecurityCodeForm";
import { SecurityPageShell } from "../ui/SecurityPageShell";
import { ChangeEmailDoneStep } from "./ChangeEmailDoneStep";
import { ChangeEmailNewStep, type EmailCodesSent } from "./ChangeEmailNewStep";
import { ChangeEmailText } from "./changeEmailContent";

type Step =
  | { readonly kind: "new"; readonly email: string }
  | {
      readonly kind: "current";
      readonly sent: EmailCodesSent;
      /** True when the person came back here because new codes were sent. */
      readonly resent: boolean;
    }
  | { readonly kind: "address"; readonly sent: EmailCodesSent }
  | { readonly kind: "done"; readonly newEmail: string };

export function ChangeEmailPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [step, setStep] = useState<Step>({ kind: "new", email: "" });

  const currentEmail = user?.email ?? "";

  function leave() {
    void navigate(routePaths.accountSecurity);
  }

  async function confirmCurrent(
    sent: EmailCodesSent,
    code: string,
  ): Promise<CodeAttempt> {
    const result = await confirmCurrentAddressCode(code);

    if (result.kind === "accepted") {
      setStep({ kind: "address", sent });
      return { kind: "accepted" };
    }

    return attemptFromProblem(result);
  }

  async function confirmAddress(
    sent: EmailCodesSent,
    code: string,
  ): Promise<CodeAttempt> {
    const result = await confirmNewAddressCode(code);

    if (result.kind === "done") {
      setStep({ kind: "done", newEmail: sent.newEmail });
      return { kind: "accepted" };
    }

    return attemptFromProblem(result);
  }

  // Asking again sends two new codes, so the first step of the codes starts over.
  async function resendCodes(
    sent: EmailCodesSent,
    returnToCurrent: boolean,
  ): Promise<ResendAttempt> {
    const result = await sendEmailChangeCodes(sent.newEmail);

    if (result.kind === "invalidEmail") {
      return { kind: "failed", message: ChangeEmailText.invalidEmail };
    }

    if (result.kind === "sent" && returnToCurrent) {
      setStep({
        kind: "current",
        sent: {
          newEmail: sent.newEmail,
          codeLength: result.codeLength,
          retryAfterSeconds: result.retryAfterSeconds,
        },
        resent: true,
      });
    }

    return resendAttemptFrom(result);
  }

  function renderStep() {
    switch (step.kind) {
      case "new":
        return (
          <ChangeEmailNewStep
            currentEmail={currentEmail}
            initialEmail={step.email}
            onSent={(sent) => setStep({ kind: "current", sent, resent: false })}
            onCancel={leave}
          />
        );
      case "current":
        return (
          <SecurityCodeForm
            key="current"
            eyebrow={ChangeEmailText.currentEyebrow}
            heading={ChangeEmailText.currentHeading}
            description={
              step.resent
                ? ChangeEmailText.currentResentDescription(currentEmail)
                : ChangeEmailText.currentDescription(currentEmail)
            }
            codeLength={step.sent.codeLength}
            initialRetryAfterSeconds={step.sent.retryAfterSeconds}
            submitLabel={ChangeEmailText.currentSubmit}
            wrongCodeNote={ChangeEmailText.wrongCodeNote}
            onConfirm={(code) => confirmCurrent(step.sent, code)}
            onResend={() => resendCodes(step.sent, false)}
            onCancel={leave}
          />
        );
      case "address":
        return (
          <SecurityCodeForm
            key="address"
            eyebrow={ChangeEmailText.addressEyebrow}
            heading={ChangeEmailText.addressHeading}
            confirmedNote={ChangeEmailText.addressConfirmed}
            description={ChangeEmailText.addressDescription(step.sent.newEmail)}
            codeLength={step.sent.codeLength}
            initialRetryAfterSeconds={step.sent.retryAfterSeconds}
            submitLabel={ChangeEmailText.addressSubmit}
            wrongCodeNote={ChangeEmailText.wrongCodeNote}
            onConfirm={(code) => confirmAddress(step.sent, code)}
            onResend={() => resendCodes(step.sent, true)}
            onCancel={leave}
          />
        );
      case "done":
        return <ChangeEmailDoneStep newEmail={step.newEmail} />;
    }
  }

  return (
    <SecurityPageShell title={ChangeEmailText.title}>
      {renderStep()}
    </SecurityPageShell>
  );
}
