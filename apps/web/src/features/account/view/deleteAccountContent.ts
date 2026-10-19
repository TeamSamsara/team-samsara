// File : /team-samsara/apps/web/src/features/account/view/deleteAccountContent.ts
// Version : 1.0.0
// Latest commit: task/web-account-security
// Author : Gerrah
// Purpose : The text of the delete-account modal: typing the handle, entering the emailed
// code, and the message shown once the account is scheduled for deletion.

export const DeleteAccountText = {
  confirmTitle: "DELETE YOUR ACCOUNT?",
  confirmDescription:
    "Your profile will be hidden now and deleted for good in 30 days. You can sign in again before then to cancel.",
  handleLabel: "Type your handle to confirm",
  handleMismatch: "That is not your handle.",
  cancel: "CANCEL",
  continue: "DELETE ACCOUNT",

  codeTitle: "CHECK YOUR EMAIL",
  codeDescription: (email: string, length: number): string =>
    `We sent a ${length}-digit code to ${email}. Enter it to delete your account.`,
  codeSubmit: "DELETE ACCOUNT",
  wrongCodeNote: "After 5 wrong tries the deletion is cancelled.",

  doneTitle: "ACCOUNT SCHEDULED FOR DELETION",
  doneMessage:
    "Your profile is hidden now and will be deleted for good in 30 days. Sign in again before then to cancel.",
} as const;
