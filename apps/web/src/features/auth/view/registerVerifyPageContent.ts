// File : /team-samsara/apps/web/src/features/auth/view/registerVerifyPageContent.ts
// Version : 1.0.0
// Latest commit: task/web-auth-codes-reset
// Author : Gerrah
// Purpose : The words on the screen that asks a new member for the code sent
// to their email address.

export const RegisterVerifyPageText = {
  title: "VERIFY YOUR EMAIL",
  describe: (codeLength: number): string =>
    `Enter the ${codeLength}-digit code we sent to your email address. It expires in 10 minutes.`,
  useAnotherAccount: "Use another account",
} as const;
