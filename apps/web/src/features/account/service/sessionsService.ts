// File : /team-samsara/apps/web/src/features/account/service/sessionsService.ts
// Version : 1.0.0
// Latest commit: task/web-account-security
// Author : Gerrah
// Purpose : Ends every session of the signed-in account, on every device. The answer
// becomes one result the screen can act on; it carries no text.

import { logOutEverywhere } from "@/features/auth/service/accountService";
import {
  failedFrom,
  type CodeFailed,
} from "@/features/auth/service/codeConfirmationService";

export type EndSessionsResult =
  /** Every session of the account was ended, this device's included. */
  { readonly kind: "done" } | CodeFailed;

export async function endAllSessions(): Promise<EndSessionsResult> {
  const outcome = await logOutEverywhere();

  if (outcome.kind !== "answer") {
    return failedFrom(outcome);
  }

  return { kind: "done" };
}
