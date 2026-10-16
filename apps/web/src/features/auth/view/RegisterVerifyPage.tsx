// File : /team-samsara/apps/web/src/features/auth/view/RegisterVerifyPage.tsx
// Version : 1.0.0
// Latest commit: task/web-auth-codes-reset
// Author : Gerrah
// Purpose : The screen that asks a new member for the code sent to their
// email address. A right code finishes the sign-up and starts the welcome
// step; leaving signs the half-made account out.

import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "@/app/useAuth";
import { routePaths } from "@/shared/constants/routePaths";
import {
  confirmRegistrationCode,
  resendRegistrationEmail,
} from "../service/codeConfirmationService";
import type { CodeAttempt, ResendAttempt } from "../types/codeAttempt";
import type { CodeScreenState } from "../types/codeScreenState";
import { AuthMessages } from "../ui/authMessagesContent";
import { AuthSplitLayout } from "../ui/AuthSplitLayout";
import { CodeScreen } from "./CodeScreen";
import { attemptFromProblem, resendAttemptFrom } from "./codeVerifyResults";
import { RegisterVerifyPageText } from "./registerVerifyPageContent";
import { useCodeScreenSetup } from "./useCodeScreenSetup";

async function resend(): Promise<ResendAttempt> {
  return resendAttemptFrom(await resendRegistrationEmail());
}

export function RegisterVerifyPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { refreshAccount, signOut } = useAuth();
  const { setup, retry, redirectTo } = useCodeScreenSetup({
    routeState: location.state,
    expected: "registrationIncomplete",
    requestCode: resendRegistrationEmail,
  });

  async function confirm(code: string): Promise<CodeAttempt> {
    const result = await confirmRegistrationCode(code, refreshAccount);

    switch (result.kind) {
      case "verified":
        void navigate(routePaths.welcome, { replace: true });
        return { kind: "accepted" };
      case "needsLoginCode": {
        const state: CodeScreenState = {
          codeLength: result.codeLength,
          retryAfterSeconds: result.retryAfterSeconds,
        };

        await refreshAccount();
        void navigate(routePaths.loginVerify, { state, replace: true });
        return { kind: "accepted" };
      }
      case "registrationIncomplete":
        return { kind: "failed", message: AuthMessages.unknown };
      case "rejected":
      case "failed":
        return attemptFromProblem(result);
    }
  }

  if (redirectTo) {
    return <Navigate to={redirectTo} replace />;
  }

  return (
    <AuthSplitLayout>
      <CodeScreen
        title={RegisterVerifyPageText.title}
        describe={RegisterVerifyPageText.describe}
        setup={setup}
        retry={retry}
        onConfirm={confirm}
        onResend={resend}
        secondaryAction={{
          label: RegisterVerifyPageText.useAnotherAccount,
          onClick: () => void signOut(),
        }}
      />
    </AuthSplitLayout>
  );
}
