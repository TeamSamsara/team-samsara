// File : /team-samsara/apps/web/src/features/auth/service/loginService.ts
// Version : 1.0.0
// Latest commit: task/web-auth-screens
// Author : Gerrah
// Purpose : Logs in with email and password: Firebase signs in, then the API
// checks the sign-in. Every outcome becomes one result the screen can act on.

import type { AuthStatus } from "@/app/AuthContext";
import { signIn, type FirebaseFailure } from "./firebaseAuthService";
import {
  establishVerifiedSession,
  type SessionFailure,
  type SessionResult,
} from "./sessionService";

export type LoginFailure =
  | "invalidCredentials"
  | "accountDisabled"
  | "notFound"
  | "rateLimited"
  | "network"
  | "unavailable"
  | "unknown";

export type LoginAttempt =
  /** Logged in: the person is a verified member. */
  | { readonly kind: "member" }
  /** A code was sent for this new device; the login code screen asks for it. */
  | {
      readonly kind: "needsCode";
      readonly codeLength: number;
      readonly retryAfterSeconds: number;
    }
  /** Registration was never confirmed; the registration code screen is next. */
  | { readonly kind: "registrationIncomplete" }
  | {
      readonly kind: "failed";
      readonly reason: LoginFailure;
      /** How long to wait before trying again, when the answer says. */
      readonly retryAfterSeconds: number | null;
    };

function failed(
  reason: LoginFailure,
  retryAfterSeconds: number | null = null,
): LoginAttempt {
  return { kind: "failed", reason, retryAfterSeconds };
}

// A wrong password and an unknown email are the same failure, so the answer
// never says which one it was.
function fromFirebase(failure: FirebaseFailure): LoginFailure {
  switch (failure) {
    case "invalidCredentials":
    case "invalidEmail":
      return "invalidCredentials";
    case "accountDisabled":
      return "accountDisabled";
    case "tooManyAttempts":
      return "rateLimited";
    case "network":
      return "network";
    case "emailInUse":
    case "weakPassword":
    case "unknown":
      return "unknown";
  }
}

function fromSessionFailure(reason: SessionFailure): LoginFailure {
  switch (reason) {
    case "rateLimited":
      return "rateLimited";
    case "unavailable":
      return "unavailable";
    case "notFound":
      return "notFound";
    case "signedOut":
    case "unknown":
      return "unknown";
  }
}

function toAttempt(result: SessionResult): LoginAttempt {
  switch (result.kind) {
    case "member":
    case "needsCode":
    case "registrationIncomplete":
      return result;
    case "failed":
      return failed(
        fromSessionFailure(result.reason),
        result.retryAfterSeconds,
      );
  }
}

export async function logInWithEmail(
  email: string,
  password: string,
  refreshAccount: () => Promise<AuthStatus>,
): Promise<LoginAttempt> {
  const signedIn = await signIn(email, password);

  if (!signedIn.ok) {
    return failed(fromFirebase(signedIn.failure));
  }

  return toAttempt(
    await establishVerifiedSession(refreshAccount, {
      forceTokenRefresh: false,
    }),
  );
}
