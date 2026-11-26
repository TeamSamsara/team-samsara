// File : /team-samsara/apps/web/src/features/auth/view/forgotPasswordStepContent.ts
// Version : 1.0.0
// Latest commit: task/web-auth-codes-reset
// Author : Gerrah
// Purpose : The words of the last two steps of "forgot password": choosing
// the new password, and the confirmation that it was changed.

export const ForgotPasswordStepText = {
  title: "CHOOSE A NEW PASSWORD",
  description: "Pick a password you don't use anywhere else.",
  passwordLabel: "New password",
  submit: "SAVE PASSWORD",
  startOver: "Start over",
  weakPassword: "That password can't be used. Choose a different one.",
  tokenExpired: "This reset has expired. Please start over.",
} as const;

export const ForgotDoneStepText = {
  title: "PASSWORD UPDATED",
  description:
    "Your password has been changed and you've been signed out everywhere. Log in with your new password.",
  login: "LOG IN",
} as const;
