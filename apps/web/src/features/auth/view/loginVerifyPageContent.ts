// File : /team-samsara/apps/web/src/features/auth/view/loginVerifyPageContent.ts
// Version : 1.0.0
// Latest commit: task/web-auth-codes-reset
// Author : Gerrah
// Purpose : The words on the screen that asks for the code sent when someone
// logs in from a device we don't recognize.

export const LoginVerifyPageText = {
  title: "VERIFY IT'S YOU",
  describe: (codeLength: number): string =>
    `We don't recognize this device. Enter the ${codeLength}-digit code we sent to your email address. It expires in 10 minutes.`,
  useAnotherAccount: "Use another account",
} as const;
