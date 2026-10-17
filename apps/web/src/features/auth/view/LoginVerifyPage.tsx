// File : /team-samsara/apps/web/src/features/auth/view/LoginVerifyPage.tsx
// Version : 1.1.0
// Latest commit: task/web-auth-onboarding-navbar
// Author : Gerrah
// Purpose : The screen that asks for the code sent when someone logs in from
// a device we don't recognize. A right code lands on the welcome step if the
// member has not finished or skipped it yet, on the home page otherwise;
// leaving signs the half-finished login out.

import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "@/app/useAuth";
import { pathAfterLogin } from "@/features/account/service/landingService";
import { routePaths } from "@/shared/constants/routePaths";
import {
  confirmLoginCode,
  resendLoginEmail,
} from "../service/codeConfirmationService";
import type { CodeAttempt, ResendAttempt } from "../types/codeAttempt";
import { AuthCenteredLayout } from "../ui/AuthCenteredLayout";
import { AuthMessages } from "../ui/authMessagesContent";
import { CodeScreen } from "./CodeScreen";
import { attemptFromProblem, resendAttemptFrom } from "./codeVerifyResults";
import { LoginVerifyPageText } from "./loginVerifyPageContent";
import { useCodeScreenSetup } from "./useCodeScreenSetup";

async function resend(): Promise<ResendAttempt> {
  return resendAttemptFrom(await resendLoginEmail());
}

export function LoginVerifyPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { refreshAccount, signOut } = useAuth();
  const { setup, retry, redirectTo } = useCodeScreenSetup({
    routeState: location.state,
    expected: "verificationRequired",
    requestCode: resendLoginEmail,
  });

  async function confirm(code: string): Promise<CodeAttempt> {
    const result = await confirmLoginCode(code, refreshAccount);

    switch (result.kind) {
      case "verified":
        void navigate(await pathAfterLogin(), { replace: true });
        return { kind: "accepted" };
      case "registrationIncomplete":
        await refreshAccount();
        void navigate(routePaths.registerVerify, { replace: true });
        return { kind: "accepted" };
      case "needsLoginCode":
        // Only a confirmed registration can answer this; not possible here.
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
    <AuthCenteredLayout>
      <CodeScreen
        title={LoginVerifyPageText.title}
        describe={LoginVerifyPageText.describe}
        setup={setup}
        retry={retry}
        onConfirm={confirm}
        onResend={resend}
        secondaryAction={{
          label: LoginVerifyPageText.useAnotherAccount,
          onClick: () => void signOut(),
        }}
      />
    </AuthCenteredLayout>
  );
}
