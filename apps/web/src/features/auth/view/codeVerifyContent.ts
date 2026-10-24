// File : /team-samsara/apps/web/src/features/auth/view/codeVerifyContent.ts
// Version : 1.0.0
// Latest commit: task/web-auth-codes-reset
// Author : Gerrah
// Purpose : The messages the register and log in code screens share for a
// step that failed: no connection, too many tries, an unfinished account.

import type { CodeFailure } from "../service/codeConfirmationService";
import { AuthMessages } from "../ui/authMessagesContent";

export const CodeVerifyMessages = {
  failure: (reason: CodeFailure, retryAfterSeconds: number | null): string => {
    switch (reason) {
      case "rateLimited":
        return AuthMessages.rateLimited(retryAfterSeconds);
      case "unavailable":
        return AuthMessages.network;
      case "notFound":
        return "This account isn't finished yet. Create your account again to continue.";
      case "signedOut":
        return "Your session has ended. Log in again to continue.";
      case "unknown":
        return AuthMessages.unknown;
    }
  },
} as const;
