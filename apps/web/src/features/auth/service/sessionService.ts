// File : /team-samsara/apps/web/src/features/auth/service/sessionService.ts
// Version : 1.0.1
// Latest commit: task/web-auth-foundation
// Author : Gerrah
// Purpose : Turns a signed-in Firebase user into a verified member session. Used after
// registration and after login: check the sign-in with the API, then refresh the account.

import type { AuthStatus } from "@/app/AuthContext";
import { auth } from "@/lib/firebase";
import { checkLogin } from "./accountService";

export type SessionFailure =
  "rateLimited" | "unavailable" | "notFound" | "signedOut" | "unknown";

export type SessionResult =
  /** The sign-in is verified and the account is a member. */
  | { readonly kind: "member" }
  /** A new-device code was sent and must be entered on the login code screen. */
  | {
      readonly kind: "needsCode";
      readonly codeLength: number;
      readonly retryAfterSeconds: number;
    }
  /** Registration was never confirmed; the registration code screen comes next. */
  | { readonly kind: "registrationIncomplete" }
  | {
      readonly kind: "failed";
      readonly reason: SessionFailure;
      readonly retryAfterSeconds: number | null;
    };

function failed(
  reason: SessionFailure,
  retryAfterSeconds: number | null = null,
): SessionResult {
  return { kind: "failed", reason, retryAfterSeconds };
}

// After registration is confirmed the server writes the member claim, so the token must
// be fetched again first; after a plain login it is already current.
async function refreshToken(): Promise<boolean> {
  try {
    await auth.currentUser?.getIdToken(true);
    return auth.currentUser !== null;
  } catch {
    return false;
  }
}

export async function establishVerifiedSession(
  refreshAccount: () => Promise<AuthStatus>,
  options: { readonly forceTokenRefresh: boolean },
): Promise<SessionResult> {
  if (options.forceTokenRefresh && !(await refreshToken())) {
    return failed("unavailable");
  }

  const outcome = await checkLogin();

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
    case "answer":
      break;
  }

  const result = outcome.value;

  // A cooldown means a code was sent a moment ago, so the same code screen applies.
  switch (result.status) {
    case "authenticated":
      return (await refreshAccount()) === "member"
        ? { kind: "member" }
        : failed("unknown");
    case "challengeRequired":
    case "cooldownActive":
      return {
        kind: "needsCode",
        codeLength: result.codeLength,
        retryAfterSeconds: result.retryAfterSeconds,
      };
    case "registrationIncomplete":
      return { kind: "registrationIncomplete" };
    case "accountNotFound":
      return failed("notFound");
    default:
      return failed("unknown");
  }
}
