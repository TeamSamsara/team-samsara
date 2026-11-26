// File : /team-samsara/apps/web/src/features/auth/service/passwordResetService.ts
// Version : 1.0.0
// Latest commit: task/web-auth-codes-reset
// Author : Gerrah
// Purpose : The three calls of "forgot password" for a signed-out person: ask
// for a code, trade the code for a one-time token, set the new password.
// Every answer becomes one result the screen can act on; none carries text.

import type { CodeRejection } from "../types/codeAttempt";
import {
  requestPasswordReset,
  setNewPassword,
  verifyPasswordResetCode,
} from "./accountService";
import {
  failed,
  failedFrom,
  type CodeFailed,
  type CodeResend,
} from "./codeConfirmationService";

export type ResetCodeCheck =
  /** The code was right. The token lives in memory only, for the last step. */
  | { readonly kind: "verified"; readonly resetToken: string }
  | { readonly kind: "rejected"; readonly reason: CodeRejection }
  | CodeFailed;

export type NewPasswordResult =
  | { readonly kind: "done" }
  /** The password breaks the rules; the token is still good. */
  | { readonly kind: "weakPassword" }
  /** The token was used or has expired; the person starts again. */
  | { readonly kind: "tokenExpired" }
  | CodeFailed;

/**
 * Asks for a reset code. The API answers the same for every address, so an
 * email without an account looks exactly like one with an account.
 */
export async function requestResetCode(email: string): Promise<CodeResend> {
  const outcome = await requestPasswordReset(email);

  if (outcome.kind !== "answer") {
    return failedFrom(outcome);
  }

  const { status, codeLength, retryAfterSeconds } = outcome.value;

  switch (status) {
    case "success":
    case "cooldownActive":
      return { kind: "sent", codeLength, retryAfterSeconds };
    case "tooManyAttempts":
      return failed("rateLimited", retryAfterSeconds);
    case "accountNotFound":
    case "invalidPassword":
    case "invalidCode":
    case "codeExpired":
    case "noPendingCode":
    case "invalidResetToken":
      return failed("unknown");
  }
}

/** Trades the emailed code for the one-time token. */
export async function checkResetCode(
  email: string,
  code: string,
): Promise<ResetCodeCheck> {
  const outcome = await verifyPasswordResetCode(email, code);

  if (outcome.kind !== "answer") {
    return failedFrom(outcome);
  }

  const { status, resetToken } = outcome.value;

  switch (status) {
    case "success":
      return resetToken === null
        ? failed("unknown")
        : { kind: "verified", resetToken };
    case "invalidCode":
    case "codeExpired":
    case "tooManyAttempts":
    case "noPendingCode":
      return { kind: "rejected", reason: status };
    case "accountNotFound":
    case "invalidPassword":
    case "cooldownActive":
    case "invalidResetToken":
      return failed("unknown");
  }
}

/** Sets the new password. A success ends every session of the account. */
export async function resetPassword(
  resetToken: string,
  newPassword: string,
): Promise<NewPasswordResult> {
  const outcome = await setNewPassword(resetToken, newPassword);

  if (outcome.kind !== "answer") {
    return failedFrom(outcome);
  }

  switch (outcome.value.status) {
    case "success":
      return { kind: "done" };
    case "invalidPassword":
      return { kind: "weakPassword" };
    case "invalidResetToken":
      return { kind: "tokenExpired" };
    case "accountNotFound":
    case "cooldownActive":
    case "invalidCode":
    case "codeExpired":
    case "tooManyAttempts":
    case "noPendingCode":
      return failed("unknown");
  }
}
