// File : /team-samsara/apps/web/src/features/account/view/changeEmailContent.ts
// Version : 1.1.0
// Latest commit: task/web-account-security
// Author : Gerrah
// Purpose : The text of the change-email page: the title, the new-address step, the two
// code steps and the confirmation that every device was signed out.

export const ChangeEmailText = {
  title: "CHANGE EMAIL",

  newEyebrow: "STEP 1 OF 3",
  newHeading: "NEW EMAIL ADDRESS",
  newDescription: (current: string): string =>
    `Your current address is ${current}. We will check both mailboxes, one after the other.`,
  newLabel: "New email address",
  newHint: "You will be signed out everywhere once the change is confirmed.",
  sameAsCurrent: "That is already your email address.",
  invalidEmail: "That email address can't be used.",
  sendCodes: "SEND CODES",

  currentEyebrow: "STEP 2 OF 3",
  currentHeading: "CHECK YOUR CURRENT EMAIL",
  currentDescription: (email: string): string =>
    `Enter the code we sent to ${email}.`,
  currentResentDescription: (email: string): string =>
    `We sent new codes to both addresses. Start again with the code we sent to ${email}.`,
  currentSubmit: "VERIFY",

  addressEyebrow: "STEP 3 OF 3",
  addressHeading: "NOW CHECK YOUR NEW EMAIL",
  addressConfirmed: "Current address confirmed",
  addressDescription: (email: string): string =>
    `Enter the code we sent to ${email}.`,
  addressSubmit: "CHANGE EMAIL",

  wrongCodeNote: "After 5 wrong tries the change is cancelled.",

  doneHeading: "EMAIL CHANGED",
  doneDescription: (maskedEmail: string): string =>
    `Your sign-in address is now ${maskedEmail}.`,
  doneNotice:
    "You were signed out on every device. Log in again with the new address. We told your old address about the change.",
} as const;
