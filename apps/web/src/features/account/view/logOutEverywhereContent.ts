// File : /team-samsara/apps/web/src/features/account/view/logOutEverywhereContent.ts
// Version : 1.0.0
// Latest commit: task/web-account-security
// Author : Gerrah
// Purpose : The text of the "sign out everywhere" modal: the question, its two buttons
// and the message shown once every session has ended.

export const LogOutEverywhereText = {
  title: "SIGN OUT EVERYWHERE?",
  description:
    "This signs you out on every device where you are logged in, this one included. You will need to log in again.",
  confirm: "SIGN OUT EVERYWHERE",
  cancel: "NOT NOW",
  doneTitle: "SIGNED OUT EVERYWHERE",
  doneMessage:
    "Log in, then choose a new password. Anyone who has your old password can no longer use it once you change it.",
} as const;
