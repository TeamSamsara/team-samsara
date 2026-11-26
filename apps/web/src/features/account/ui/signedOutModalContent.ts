// File : /team-samsara/apps/web/src/features/account/ui/signedOutModalContent.ts
// Version : 1.0.0
// Latest commit: task/web-account-security
// Author : Gerrah
// Purpose : The text of the "you were signed out" modal and how long it waits before
// taking the person to the home page.

export const signedOutRedirectSeconds = 5;

export const SignedOutModalText = {
  login: "LOG IN",
  redirectIn: (seconds: number): string =>
    `You will be taken to the home page in ${seconds} ${seconds === 1 ? "second" : "seconds"}.`,
} as const;
