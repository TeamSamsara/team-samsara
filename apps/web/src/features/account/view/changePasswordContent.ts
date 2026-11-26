// File : /team-samsara/apps/web/src/features/account/view/changePasswordContent.ts
// Version : 1.1.0
// Latest commit: task/web-account-security
// Author : Gerrah
// Purpose : The text of the change-password page: the title, the new-password step,
// the code step and the confirmation that every device was signed out.

import {
  passwordMaxLength,
  passwordMinLength,
} from "@/features/auth/validation/authValidation";

const codeExpiryMinutes = 10;

export const ChangePasswordText = {
  title: "CHANGE PASSWORD",

  newHeading: "CHOOSE A NEW PASSWORD",
  newDescription: `${passwordMinLength} to ${passwordMaxLength} characters. We email you a code before anything changes.`,
  newLabel: "New password",
  confirmLabel: "Confirm new password",
  confirmRequired: "Confirm your new password.",
  match: "Passwords match",
  mismatch: "Passwords do not match.",
  weakPassword: "That password can't be used. Choose a different one.",
  sendCode: "SEND CODE",

  codeHeading: "ENTER THE CODE",
  codeDescription: (email: string, length: number): string =>
    `We sent a ${length}-digit code to ${email}. It expires in ${codeExpiryMinutes} minutes.`,
  codeSubmit: "CHANGE PASSWORD",
  wrongCodeNote: "After 5 wrong tries the change is cancelled.",

  doneTitle: "PASSWORD CHANGED",
  doneDescription:
    "For your security you were signed out on every device. Log in again with your new password. We also emailed you a notice about this change.",
} as const;
