// File : /team-samsara/apps/web/src/features/auth/ui/codeVerificationFormContent.ts
// Version : 1.0.0
// Latest commit: task/web-auth-codes-reset
// Author : Gerrah
// Purpose : The text of the code form: its button, the resend link and its
// countdown, and the message for each reason a code is turned down.

import type { CodeRejection } from "../types/codeAttempt";

function clock(totalSeconds: number): string {
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = String(totalSeconds % 60).padStart(2, "0");
  return `${minutes}:${seconds}`;
}

export const CodeVerificationText = {
  codeLabel: "Verification code",
  submit: "VERIFY",
  resend: "Resend code",
  resendIn: (seconds: number): string => `Resend code in ${clock(seconds)}`,
  resent: "A new code is on its way.",
  incomplete: (length: number): string =>
    `Enter all ${length} digits of the code.`,
  rejection: (reason: CodeRejection): string => {
    switch (reason) {
      case "invalidCode":
        return "That code isn't right. Check it and try again.";
      case "codeExpired":
        return "That code has expired. Request a new one.";
      case "tooManyAttempts":
        return "Too many wrong attempts. Request a new code.";
      case "noPendingCode":
        return "There is no code waiting. Request a new one.";
    }
  },
} as const;
