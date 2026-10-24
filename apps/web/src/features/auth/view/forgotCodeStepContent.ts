// File : /team-samsara/apps/web/src/features/auth/view/forgotCodeStepContent.ts
// Version : 1.0.0
// Latest commit: task/web-auth-codes-reset
// Author : Gerrah
// Purpose : The words of the second step of "forgot password", where the
// person enters the code sent to their email address.

export const ForgotCodeStepText = {
  title: "CHECK YOUR EMAIL",
  // Worded so it is true whether or not the address has an account.
  describe: (email: string, codeLength: number): string =>
    `If there is an account for ${email}, we sent it a ${codeLength}-digit code. It expires in 10 minutes.`,
  useAnotherEmail: "Use a different email",
} as const;
