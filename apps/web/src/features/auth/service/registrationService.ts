// File : /team-samsara/apps/web/src/features/auth/service/registrationService.ts
// Version : 1.0.0
// Latest commit: task/web-auth-screens
// Author : Gerrah
// Purpose : The first half of registration: create the Firebase account, then ask the API to
// send the verification code. Every way it can go wrong becomes one short reason the
// screen can choose a message for. Verifying the code is the next screen's job.

import type { RegistrationCodeResult } from "../types/authTypes";
import type { ApiOutcome } from "./accountOutcome";
import { startRegistration } from "./accountService";
import {
  createAccount,
  signIn,
  type FirebaseFailure,
  type FirebaseResult,
} from "./firebaseAuthService";

export type RegistrationFailure =
  | "emailInUse"
  | "alreadyRegistered"
  | "invalidEmail"
  | "weakPassword"
  | "rateLimited"
  | "network"
  | "unavailable"
  | "unknown";

export type RegistrationAttempt =
  /** The code is on its way: the screen asks for it next. */
  | {
      readonly kind: "codeSent";
      readonly codeLength: number;
      readonly retryAfterSeconds: number;
    }
  | {
      readonly kind: "failed";
      readonly reason: RegistrationFailure;
      /** How long to wait before trying again, when the answer says. */
      readonly retryAfterSeconds: number | null;
    };

function failed(
  reason: RegistrationFailure,
  retryAfterSeconds: number | null = null,
): RegistrationAttempt {
  return { kind: "failed", reason, retryAfterSeconds };
}

function fromFirebase(failure: FirebaseFailure): RegistrationFailure {
  switch (failure) {
    case "emailInUse":
      return "emailInUse";
    case "invalidEmail":
      return "invalidEmail";
    case "weakPassword":
      return "weakPassword";
    case "tooManyAttempts":
      return "rateLimited";
    case "network":
      return "network";
    case "invalidCredentials":
    case "accountDisabled":
    case "unknown":
      return "unknown";
  }
}

// Creates the account. When the email already exists, the same password signs in instead:
// that is someone who left halfway through registering and is picking it back up.
async function openFirebaseSession(
  email: string,
  password: string,
): Promise<FirebaseResult> {
  const created = await createAccount(email, password);

  if (created.ok || created.failure !== "emailInUse") {
    return created;
  }

  const signedIn = await signIn(email, password);
  return signedIn.ok ? signedIn : { ok: false, failure: "emailInUse" };
}

function toAttempt(
  outcome: ApiOutcome<RegistrationCodeResult>,
): RegistrationAttempt {
  switch (outcome.kind) {
    case "answer": {
      const { status, codeLength, retryAfterSeconds } = outcome.value;

      if (status === "success" || status === "cooldownActive") {
        return { kind: "codeSent", codeLength, retryAfterSeconds };
      }

      if (status === "alreadyRegistered") {
        return failed("alreadyRegistered");
      }

      return status === "tooManyAttempts"
        ? failed("rateLimited")
        : failed("unknown");
    }
    case "rateLimited":
      return failed("rateLimited", outcome.retryAfterSeconds);
    case "unauthorized":
    case "forbidden":
      return failed("unknown");
    case "unknown":
    case "unavailable":
      return failed("unavailable");
  }
}

export async function registerWithEmail(
  email: string,
  password: string,
): Promise<RegistrationAttempt> {
  const session = await openFirebaseSession(email, password);

  if (!session.ok) {
    return failed(fromFirebase(session.failure));
  }

  return toAttempt(await startRegistration());
}
