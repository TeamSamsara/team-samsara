// File : /team-samsara/apps/web/src/features/auth/validation/authValidationContent.ts
// Version : 1.0.0
// Latest commit: task/web-auth-screens
// Author : Gerrah
// Purpose : The messages shown under a form field when its value is not acceptable.

export const AuthValidationText = {
  emailRequired: "Enter your email address.",
  emailInvalid: "Enter a valid email address.",
  passwordRequired: "Enter your password.",
  passwordTooShort: (minLength: number) =>
    `Use at least ${minLength} characters.`,
  passwordTooLong: (maxLength: number) =>
    `Use at most ${maxLength} characters.`,
} as const;
