// File : /team-samsara/apps/web/src/features/auth/view/loginPageContent.ts
// Version : 1.0.0
// Latest commit: task/web-auth-screens
// Author : Gerrah
// Purpose : The text of the Log in screen, including the message for each
// way logging in can fail.

import type { LoginFailure } from "../service/loginService";
import { AuthMessages } from "../ui/authMessagesContent";

export const LoginPageText = {
  title: "LOG INTO YOUR ACCOUNT",
  emailLabel: "Email address",
  passwordLabel: "Password",
  submit: "LOG IN",
  forgotPasswordLink: "Forgot your password?",
  registerPrompt: "Not a member yet?",
  registerLink: "Create an account",
  failureMessage: (
    reason: LoginFailure,
    retryAfterSeconds: number | null,
  ): string => {
    switch (reason) {
      case "invalidCredentials":
        return "Incorrect email or password.";
      case "accountDisabled":
        return "This account has been disabled. Contact us if you think this is a mistake.";
      case "notFound":
        return "This account isn't finished yet. Create your account again to continue.";
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
