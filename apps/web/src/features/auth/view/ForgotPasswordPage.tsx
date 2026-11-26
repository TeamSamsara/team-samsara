// File : /team-samsara/apps/web/src/features/auth/view/ForgotPasswordPage.tsx
// Version : 1.0.0
// Latest commit: task/web-auth-codes-reset
// Author : Gerrah
// Purpose : The "forgot password" screen. It runs four steps in one place:
// the email address, the emailed code, the new password and the confirmation.
// The one-time token lives only in this page's memory and is gone as soon as
// the person starts over, finishes or leaves.

import { useState } from "react";
import { AuthSplitLayout } from "../ui/AuthSplitLayout";
import { ForgotCodeStep } from "./ForgotCodeStep";
import { ForgotDoneStep } from "./ForgotDoneStep";
import { ForgotEmailStep, type ResetCodeSent } from "./ForgotEmailStep";
import { ForgotPasswordStep } from "./ForgotPasswordStep";

type Step =
  | { readonly kind: "email"; readonly email: string }
  | { readonly kind: "code"; readonly sent: ResetCodeSent }
  | { readonly kind: "password"; readonly resetToken: string }
  | { readonly kind: "done" };

export function ForgotPasswordPage() {
  const [step, setStep] = useState<Step>({ kind: "email", email: "" });

  function renderStep() {
    switch (step.kind) {
      case "email":
        return (
          <ForgotEmailStep
            initialEmail={step.email}
            onSent={(sent) => setStep({ kind: "code", sent })}
          />
        );
      case "code":
        return (
          <ForgotCodeStep
            sent={step.sent}
            onVerified={(resetToken) =>
              setStep({ kind: "password", resetToken })
            }
            onChangeEmail={() =>
              setStep({ kind: "email", email: step.sent.email })
            }
          />
        );
      case "password":
        return (
          <ForgotPasswordStep
            resetToken={step.resetToken}
            onDone={() => setStep({ kind: "done" })}
            onStartOver={() => setStep({ kind: "email", email: "" })}
          />
        );
      case "done":
        return <ForgotDoneStep />;
    }
  }

  return <AuthSplitLayout>{renderStep()}</AuthSplitLayout>;
}
