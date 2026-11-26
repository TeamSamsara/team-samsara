// File : /team-samsara/apps/web/src/features/auth/view/ForgotCodeStep.tsx
// Version : 1.0.0
// Latest commit: task/web-auth-codes-reset
// Author : Gerrah
// Purpose : The second step of "forgot password": the code sent to the email
// address. A right code hands a one-time token to the last step; it is kept in
// memory only and never written to storage or the address bar.

import {
  checkResetCode,
  requestResetCode,
} from "../service/passwordResetService";
import type { CodeAttempt, ResendAttempt } from "../types/codeAttempt";
import { CodeVerificationForm } from "../ui/CodeVerificationForm";
import { attemptFromProblem, resendAttemptFrom } from "./codeVerifyResults";
import { ForgotCodeStepText } from "./forgotCodeStepContent";
import type { ResetCodeSent } from "./ForgotEmailStep";

interface ForgotCodeStepProps {
  readonly sent: ResetCodeSent;
  readonly onVerified: (resetToken: string) => void;
  /** Back to the first step, to correct the address. */
  readonly onChangeEmail: () => void;
}

export function ForgotCodeStep({
  sent,
  onVerified,
  onChangeEmail,
}: ForgotCodeStepProps) {
  async function confirm(code: string): Promise<CodeAttempt> {
    const result = await checkResetCode(sent.email, code);

    if (result.kind === "verified") {
      onVerified(result.resetToken);
      return { kind: "accepted" };
    }

    return attemptFromProblem(result);
  }

  async function resend(): Promise<ResendAttempt> {
    return resendAttemptFrom(await requestResetCode(sent.email));
  }

  return (
    <CodeVerificationForm
      title={ForgotCodeStepText.title}
      description={ForgotCodeStepText.describe(sent.email, sent.codeLength)}
      codeLength={sent.codeLength}
      initialRetryAfterSeconds={sent.retryAfterSeconds}
      onConfirm={confirm}
      onResend={resend}
      secondaryAction={{
        label: ForgotCodeStepText.useAnotherEmail,
        onClick: onChangeEmail,
      }}
    />
  );
}
