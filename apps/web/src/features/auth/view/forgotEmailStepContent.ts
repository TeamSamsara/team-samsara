// File : /team-samsara/apps/web/src/features/auth/view/forgotEmailStepContent.ts
// Version : 1.0.0
// Latest commit: task/web-auth-codes-reset
// Author : Gerrah
// Purpose : The words of the first step of "forgot password", where the
// person says which account needs a new password.

export const ForgotEmailStepText = {
  title: "FORGOT YOUR PASSWORD?",
  description:
    "Enter the email address of your account. We'll send you a code to reset your password.",
  emailLabel: "Email address",
  submit: "SEND CODE",
  backToLogin: "Back to log in",
} as const;
