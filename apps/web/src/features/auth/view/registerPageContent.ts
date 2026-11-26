// File : /team-samsara/apps/web/src/features/auth/view/registerPageContent.ts
// Version : 1.1.0
// Latest commit: task/web-auth-screens
// Author : Gerrah
// Purpose : The text of the Register screen, including the message for each
// way creating an account can fail.

import type { RegistrationFailure } from "../service/registrationService";
import { AuthMessages } from "../ui/authMessagesContent";
import { passwordMinLength } from "../validation/authValidation";
import { AuthValidationText } from "../validation/authValidationContent";

export const RegisterPageText = {
  title: "CREATE YOUR ACCOUNT",
  emailLabel: "Email address",
  passwordLabel: "Password",
  submit: "CREATE ACCOUNT",
  loginPrompt: "Already a member?",
  loginLink: "Log in",
  failureMessage: (
    reason: RegistrationFailure,
    retryAfterSeconds: number | null,
  ): string => {
    switch (reason) {
      case "emailInUse":
        return "We couldn't create an account with these details. If you already have one, log in instead.";
      case "alreadyRegistered":
        return "You already have an account. Log in instead.";
      case "invalidEmail":
        return AuthValidationText.emailInvalid;
      case "weakPassword":
        return AuthValidationText.passwordTooShort(passwordMinLength);
      case "rateLimited":
        return AuthMessages.rateLimited(retryAfterSeconds);
      case "network":
        return AuthMessages.network;
      case "unavailable":
        return AuthMessages.unavailable;
      case "unknown":
        return AuthMessages.unknown;
    }
  },
} as const;
