// File : /team-samsara/apps/web/src/features/auth/view/useCodeScreenSetup.ts
// Version : 2.0.0
// Latest commit: task/web-auth-codes-reset
// Author : Gerrah
// Purpose : Gives a code screen what it needs to start: the code length, the
// wait before a resend, and where to send a visitor who does not belong there.
// After a reload the length is gone, so a code is asked for once the sign-in
// has loaded and the screen shows it is waiting.

import { useCallback, useEffect, useState } from "react";
import { useAuth } from "@/app/useAuth";
import { routePaths } from "@/shared/constants/routePaths";
import type { CodeResend } from "../service/codeConfirmationService";
import { readCodeScreenState } from "../types/codeScreenState";
import { CodeVerifyMessages } from "./codeVerifyContent";

export type CodeSetup =
  | { readonly kind: "loading" }
  | {
      readonly kind: "ready";
      readonly codeLength: number;
      readonly retryAfterSeconds: number;
    }
  | { readonly kind: "failed"; readonly message: string };

/** The half-signed-in state each code screen is for. */
type ExpectedState = "registrationIncomplete" | "verificationRequired";

interface CodeScreenSetupOptions {
  /** location.state of the page, where the previous screen left the length. */
  readonly routeState: unknown;
  readonly expected: ExpectedState;
  readonly requestCode: () => Promise<CodeResend>;
}

export function useCodeScreenSetup({
  routeState,
  expected,
  requestCode,
}: CodeScreenSetupOptions): {
  readonly setup: CodeSetup;
  readonly retry: () => void;
  /** Where to send this visitor instead, or null to show the screen. */
  readonly redirectTo: string | null;
} {
  const { status } = useAuth();
  const [setup, setSetup] = useState<CodeSetup>(() => {
    const known = readCodeScreenState(routeState);
    return known ? { kind: "ready", ...known } : { kind: "loading" };
  });

  // Asking before the sign-in has loaded would go out without its token.
  const canRequest = status === expected || status === "unavailable";

  useEffect(() => {
    if (setup.kind !== "loading" || !canRequest) {
      return;
    }

    let cancelled = false;

    void requestCode().then((result) => {
      if (cancelled) {
        return;
      }

      setSetup(
        result.kind === "sent"
          ? {
              kind: "ready",
              codeLength: result.codeLength,
              retryAfterSeconds: result.retryAfterSeconds,
            }
          : {
              kind: "failed",
              message: CodeVerifyMessages.failure(
                result.reason,
                result.retryAfterSeconds,
              ),
            },
      );
    });

    return () => {
      cancelled = true;
    };
  }, [setup.kind, canRequest, requestCode]);

  const retry = useCallback(() => {
    setSetup({ kind: "loading" });
  }, []);

  return { setup, retry, redirectTo: redirectFor(status, expected, setup) };
}

// Signing out always leaves the screen. Any other mismatch only matters before
// the screen is ready: once it is, the code form itself decides where to go.
function redirectFor(
  status: ReturnType<typeof useAuth>["status"],
  expected: ExpectedState,
  setup: CodeSetup,
): string | null {
  if (status === "anonymous") {
    return routePaths.login;
  }

  if (setup.kind === "ready" || status === expected) {
    return null;
  }

  switch (status) {
    case "registrationIncomplete":
      return routePaths.registerVerify;
    case "verificationRequired":
      return routePaths.loginVerify;
    case "member":
      return routePaths.home;
    case "loading":
    case "unavailable":
      return null;
  }
}
