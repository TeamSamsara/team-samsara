// File : /team-samsara/apps/web/src/features/auth/validation/authValidation.ts
// Version : 1.0.0
// Latest commit: task/web-auth-screens
// Author : Gerrah
// Purpose : The rules a form checks before calling the API. Each returns the message to
// show, or null when the value is fine. The API repeats every check; these only save a trip.

import { AuthValidationText } from "./authValidationContent";

/** Longest valid email address; the API refuses anything longer. */
export const emailMaxLength = 254;

/** Password length limits; they mirror Identity:Password in the API's settings. */
export const passwordMinLength = 8;
export const passwordMaxLength = 128;

const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function validateEmail(value: string): string | null {
  const email = value.trim();

  if (email.length === 0) {
    return AuthValidationText.emailRequired;
  }

  if (email.length > emailMaxLength || !emailPattern.test(email)) {
    return AuthValidationText.emailInvalid;
  }

  return null;
}

/** For choosing a new password: length must be within the limits. */
export function validateNewPassword(value: string): string | null {
  if (value.length === 0) {
    return AuthValidationText.passwordRequired;
  }

  if (value.length < passwordMinLength) {
    return AuthValidationText.passwordTooShort(passwordMinLength);
  }

  if (value.length > passwordMaxLength) {
    return AuthValidationText.passwordTooLong(passwordMaxLength);
  }

  return null;
}

/** For logging in: only presence is checked, so an older password is never refused here. */
export function validateExistingPassword(value: string): string | null {
  return value.length === 0 ? AuthValidationText.passwordRequired : null;
}
