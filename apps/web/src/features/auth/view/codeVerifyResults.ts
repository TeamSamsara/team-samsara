// File : /team-samsara/apps/web/src/features/auth/view/codeVerifyResults.ts
// Version : 1.0.0
// Latest commit: task/web-auth-codes-reset
// Author : Gerrah
// Purpose : Turns what the code service answered into what the code form
// shows. Shared by the register and log in code screens, which handle the
// successful answers themselves because each goes somewhere different.

import type {
  CodeConfirmation,
  CodeResend,
} from "../service/codeConfirmationService";
import type { CodeAttempt, ResendAttempt } from "../types/codeAttempt";
import { CodeVerificationText } from "../ui/codeVerificationFormContent";
import { CodeVerifyMessages } from "./codeVerifyContent";

/** The two answers that mean the code was not accepted. */
type Problem = Extract<CodeConfirmation, { kind: "rejected" | "failed" }>;

export function attemptFromProblem(problem: Problem): CodeAttempt {
  if (problem.kind === "rejected") {
    return {
      kind: "wrongCode",
      message: CodeVerificationText.rejection(problem.reason),
    };
  }

  return {
    kind: "failed",
    message: CodeVerifyMessages.failure(
      problem.reason,
      problem.retryAfterSeconds,
    ),
  };
}

export function resendAttemptFrom(result: CodeResend): ResendAttempt {
  if (result.kind === "sent") {
    return { kind: "waiting", retryAfterSeconds: result.retryAfterSeconds };
  }

  return {
    kind: "failed",
    message: CodeVerifyMessages.failure(
      result.reason,
      result.retryAfterSeconds,
    ),
  };
}
