// File : /team-samsara/apps/web/src/features/account/service/emailChangeService.ts
// Version : 1.1.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : The calls of "change email" for a signed-in member: ask for the two codes
// with the new address, then send the code from the current address and the code from
// the new address, one at a time. Every answer becomes one result the screen can act
// on; none carries text.

import {
  confirmEmailChange,
  requestEmailChange,
} from "@/features/auth/service/accountService";
import {
  failed,
  failedFrom,
  type CodeFailed,
} from "@/features/auth/service/codeConfirmationService";
import type { CodeRejection } from "@/features/auth/types/codeAttempt";

export type EmailCodesRequest =
  /** The codes were sent, or were sent a moment ago. */
  | {
      readonly kind: "sent";
      readonly codeLength: number;
      readonly retryAfterSeconds: number;
    }
  /** The new address is not a usable address; nothing was sent. */
  | { readonly kind: "invalidEmail" }
  /** Another account already uses the new address; nothing was sent. */
  | { readonly kind: "emailTaken" }
  /** The new address is the one the account already has; nothing was sent. */
  | { readonly kind: "sameAsCurrent" }
  | CodeFailed;

export type CurrentCodeResult =
  /** The code from the current address was right; the new address is next. */
  | { readonly kind: "accepted" }
  | { readonly kind: "rejected"; readonly reason: CodeRejection }
  | CodeFailed;

export type NewCodeResult =
  /** Both codes were right: the address changed and every session ended. */
  | { readonly kind: "done" }
  | { readonly kind: "rejected"; readonly reason: CodeRejection }
  | CodeFailed;

/**
 * Emails one code to the current address and one to the new address. The API
 * checks the new address first, so a taken or unchanged address is refused
 * before anything is sent.
 */
export async function sendEmailChangeCodes(
  newEmail: string,
): Promise<EmailCodesRequest> {
  const outcome = await requestEmailChange(newEmail);

  if (outcome.kind !== "answer") {
    return failedFrom(outcome);
  }

  const { status, codeLength, retryAfterSeconds } = outcome.value;

  switch (status) {
    case "success":
    case "cooldownActive":
      return { kind: "sent", codeLength, retryAfterSeconds };
    case "invalidEmail":
      return { kind: "invalidEmail" };
    case "emailTaken":
      return { kind: "emailTaken" };
    case "sameAsCurrentEmail":
      return { kind: "sameAsCurrent" };
    case "tooManyAttempts":
      return failed("rateLimited", retryAfterSeconds);
    case "accountNotFound":
      return failed("notFound");
    case "codeAccepted":
    case "invalidCode":
    case "noPendingRequest":
      return failed("unknown");
  }
}

/** Sends the code that arrived at the current address. */
export async function confirmCurrentAddressCode(
  code: string,
): Promise<CurrentCodeResult> {
  const outcome = await confirmEmailChange({ oldCode: code });

  if (outcome.kind !== "answer") {
    return failedFrom(outcome);
  }

  const status = outcome.value.status;

  switch (status) {
    case "codeAccepted":
      return { kind: "accepted" };
    case "invalidCode":
    case "tooManyAttempts":
      return { kind: "rejected", reason: status };
    case "noPendingRequest":
      return { kind: "rejected", reason: "noPendingCode" };
    case "accountNotFound":
      return failed("notFound");
    case "success":
    case "invalidEmail":
    case "emailTaken":
    case "sameAsCurrentEmail":
    case "cooldownActive":
      return failed("unknown");
  }
}

/** Sends the code that arrived at the new address, which finishes the change. */
export async function confirmNewAddressCode(
  code: string,
): Promise<NewCodeResult> {
  const outcome = await confirmEmailChange({ newCode: code });

  if (outcome.kind !== "answer") {
    return failedFrom(outcome);
  }

  const status = outcome.value.status;

  switch (status) {
    case "success":
      return { kind: "done" };
    case "invalidCode":
    case "tooManyAttempts":
      return { kind: "rejected", reason: status };
    case "noPendingRequest":
      return { kind: "rejected", reason: "noPendingCode" };
    case "accountNotFound":
      return failed("notFound");
    case "codeAccepted":
    case "invalidEmail":
    case "emailTaken":
    case "sameAsCurrentEmail":
    case "cooldownActive":
      return failed("unknown");
  }
}
