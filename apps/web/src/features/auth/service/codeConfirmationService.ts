// File : /team-samsara/apps/web/src/features/auth/service/codeConfirmationService.ts
// Version : 1.2.0
// Latest commit: task/web-auth-codes-reset
// Author : Gerrah
// Purpose : Sends the emailed code for registration or login, and asks for a
// new one. Every answer becomes one result the code screens can act on; it
// carries no text, the screens choose the words.

import type { AuthStatus } from "@/app/AuthContext";
import type { CodeRejection } from "../types/codeAttempt";
import {
  confirmLogin,
  confirmRegistration,
  resendLoginCode,
  resendRegistrationCode,
} from "./accountService";
import type { ApiOutcome } from "./accountOutcome";
import {
  establishVerifiedSession,
  type SessionFailure,
  type SessionResult,
} from "./sessionService";

export type CodeFailure = SessionFailure;

export interface CodeFailed {
  readonly kind: "failed";
  readonly reason: CodeFailure;
  /** How long to wait before trying again, when the answer says. */
  readonly retryAfterSeconds: number | null;
}

export type CodeConfirmation =
  /** The code was right and the account is now a signed-in member. */
  | { readonly kind: "verified" }
  /** Registration is done, but this device still needs a login code. */
  | {
      readonly kind: "needsLoginCode";
      readonly codeLength: number;
      readonly retryAfterSeconds: number;
    }
  /** Registration was never confirmed; the registration code screen is next. */
  | { readonly kind: "registrationIncomplete" }
  | { readonly kind: "rejected"; readonly reason: CodeRejection }
  | CodeFailed;

export type CodeResend =
  /** A new code was sent, or one was sent a moment ago. */
  | {
      readonly kind: "sent";
      readonly codeLength: number;
      readonly retryAfterSeconds: number;
    }
  | CodeFailed;

type NotAnswer = Exclude<ApiOutcome<unknown>, { readonly kind: "answer" }>;

export function failed(
  reason: CodeFailure,
  retryAfterSeconds: number | null = null,
): CodeFailed {
  return { kind: "failed", reason, retryAfterSeconds };
}

export function failedFrom(outcome: NotAnswer): CodeFailed {
  switch (outcome.kind) {
    case "rateLimited":
      return failed("rateLimited", outcome.retryAfterSeconds);
    case "unavailable":
      return failed("unavailable");
    case "unauthorized":
      return failed("signedOut");
    case "forbidden":
    case "unknown":
      return failed("unknown");
  }
}

function fromSession(session: SessionResult): CodeConfirmation {
  switch (session.kind) {
    case "member":
      return { kind: "verified" };
    case "needsCode":
      return {
        kind: "needsLoginCode",
        codeLength: session.codeLength,
        retryAfterSeconds: session.retryAfterSeconds,
      };
    case "registrationIncomplete":
      // Right after a confirmed registration this should not happen.
      return failed("unknown");
    case "failed":
      return failed(session.reason, session.retryAfterSeconds);
  }
}

/** Confirms the registration code, then signs the new member in. */
export async function confirmRegistrationCode(
  code: string,
  refreshAccount: () => Promise<AuthStatus>,
): Promise<CodeConfirmation> {
  const outcome = await confirmRegistration(code);

  if (outcome.kind !== "answer") {
    return failedFrom(outcome);
  }

  const status = outcome.value.status;

  switch (status) {
    case "success":
    case "alreadyRegistered":
      return fromSession(
        await establishVerifiedSession(refreshAccount, {
          forceTokenRefresh: true,
        }),
      );
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

/** Confirms the new-device login code. */
export async function confirmLoginCode(
  code: string,
  refreshAccount: () => Promise<AuthStatus>,
): Promise<CodeConfirmation> {
  const outcome = await confirmLogin(code);

  if (outcome.kind !== "answer") {
    return failedFrom(outcome);
  }

  const status = outcome.value.status;

  switch (status) {
    case "authenticated":
      return (await refreshAccount()) === "member"
        ? { kind: "verified" }
        : failed("unknown");
    case "invalidCode":
    case "codeExpired":
    case "tooManyAttempts":
    case "noPendingCode":
      return { kind: "rejected", reason: status };
    case "registrationIncomplete":
      return { kind: "registrationIncomplete" };
    case "accountNotFound":
      return failed("notFound");
    case "challengeRequired":
    case "cooldownActive":
      return failed("unknown");
  }
}

/** Asks for a new registration code. */
export async function resendRegistrationEmail(): Promise<CodeResend> {
  const outcome = await resendRegistrationCode();

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
    case "alreadyRegistered":
    case "invalidCode":
    case "codeExpired":
    case "noPendingCode":
      return failed("unknown");
  }
}

/** Asks for a new login code. */
export async function resendLoginEmail(): Promise<CodeResend> {
  const outcome = await resendLoginCode();

  if (outcome.kind !== "answer") {
    return failedFrom(outcome);
  }

  const { status, codeLength, retryAfterSeconds } = outcome.value;

  switch (status) {
    case "challengeRequired":
    case "cooldownActive":
      return { kind: "sent", codeLength, retryAfterSeconds };
    case "tooManyAttempts":
      return failed("rateLimited", retryAfterSeconds);
    case "accountNotFound":
      return failed("notFound");
    case "authenticated":
    case "registrationIncomplete":
    case "invalidCode":
    case "codeExpired":
    case "noPendingCode":
      return failed("unknown");
  }
}
