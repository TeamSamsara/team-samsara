// File : /team-samsara/apps/web/src/features/account/service/passwordChangeService.ts
// Version : 1.0.0
// Latest commit: task/web-account-security
// Author : Gerrah
// Purpose : The two calls of "change password" for a signed-in member: ask for a
// code with the new password, then send the code with it. Every answer becomes
// one result the screen can act on; none carries text.

import {
  changePassword,
  requestPasswordChangeCode,
} from "@/features/auth/service/accountService";
import {
  failed,
  failedFrom,
  type CodeFailed,
} from "@/features/auth/service/codeConfirmationService";
import type { CodeRejection } from "@/features/auth/types/codeAttempt";

export type PasswordCodeRequest =
  /** A code was sent, or one was sent a moment ago. */
  | {
      readonly kind: "sent";
      readonly codeLength: number;
      readonly retryAfterSeconds: number;
    }
  /** The password breaks the rules; nothing was sent. */
  | { readonly kind: "weakPassword" }
  | CodeFailed;

export type PasswordChangeResult =
  /** The password changed and every session of the account was ended. */
  | { readonly kind: "done" }
  | { readonly kind: "rejected"; readonly reason: CodeRejection }
  /** The password broke the rules when it was sent with the code. */
  | { readonly kind: "weakPassword" }
  | CodeFailed;

/** Checks the new password and emails a code to the account address. */
export async function sendPasswordChangeCode(
  newPassword: string,
): Promise<PasswordCodeRequest> {
  const outcome = await requestPasswordChangeCode(newPassword);

  if (outcome.kind !== "answer") {
    return failedFrom(outcome);
  }

  const { status, codeLength, retryAfterSeconds } = outcome.value;

  switch (status) {
    case "success":
    case "cooldownActive":
      return { kind: "sent", codeLength, retryAfterSeconds };
    case "invalidPassword":
      return { kind: "weakPassword" };
    case "tooManyAttempts":
      return failed("rateLimited", retryAfterSeconds);
    case "accountNotFound":
      return failed("notFound");
    case "invalidCode":
    case "codeExpired":
    case "noPendingCode":
    case "invalidResetToken":
      return failed("unknown");
  }
}

/** Changes the password with the emailed code. */
export async function confirmPasswordChange(
  newPassword: string,
  code: string,
): Promise<PasswordChangeResult> {
  const outcome = await changePassword(newPassword, code);

  if (outcome.kind !== "answer") {
    return failedFrom(outcome);
  }

  const status = outcome.value.status;

  switch (status) {
    case "success":
      return { kind: "done" };
    case "invalidCode":
    case "codeExpired":
    case "tooManyAttempts":
    case "noPendingCode":
      return { kind: "rejected", reason: status };
    case "invalidPassword":
      return { kind: "weakPassword" };
    case "accountNotFound":
      return failed("notFound");
    case "cooldownActive":
    case "invalidResetToken":
      return failed("unknown");
  }
}
