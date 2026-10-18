// File : /team-samsara/apps/web/src/features/account/service/accountDeletionService.ts
// Version : 1.0.0
// Latest commit: task/web-account-security
// Author : Gerrah
// Purpose : The two calls of "delete account" for a signed-in member: ask for a code,
// then send it to confirm. Every answer becomes one result the screen can act on; none
// carries text.

import {
  deleteAccount,
  requestAccountDeletionCode,
} from "@/features/auth/service/accountService";
import {
  failed,
  failedFrom,
  type CodeFailed,
} from "@/features/auth/service/codeConfirmationService";
import type { CodeRejection } from "@/features/auth/types/codeAttempt";

export type DeletionCodeRequest =
  /** A code was sent, or one was sent a moment ago. */
  | {
      readonly kind: "sent";
      readonly codeLength: number;
      readonly retryAfterSeconds: number;
    }
  | CodeFailed;

export type DeletionResult =
  /** The account is hidden now and will be removed after the recovery window. */
  | { readonly kind: "done" }
  | { readonly kind: "rejected"; readonly reason: CodeRejection }
  | CodeFailed;

/** Emails a deletion code to the account address. */
export async function sendDeletionCode(): Promise<DeletionCodeRequest> {
  const outcome = await requestAccountDeletionCode();

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
      return failed("notFound");
    case "invalidCode":
    case "codeExpired":
    case "noPendingCode":
      return failed("unknown");
  }
}

/** Deletes the account with the emailed code. */
export async function confirmDeletion(code: string): Promise<DeletionResult> {
  const outcome = await deleteAccount(code);

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
    case "accountNotFound":
      return failed("notFound");
    case "cooldownActive":
      return failed("unknown");
  }
}
