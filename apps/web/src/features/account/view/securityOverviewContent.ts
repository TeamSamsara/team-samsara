// File : /team-samsara/apps/web/src/features/account/view/securityOverviewContent.ts
// Version : 1.0.0
// Latest commit: task/web-account-security
// Author : Gerrah
// Purpose : The text of the account security overview: the title, the back link
// and the four cards (password, email, sessions, delete account).

export const SecurityOverviewText = {
  back: "← Back to my profile",
  title: "ACCOUNT SECURITY",
  passwordTitle: "PASSWORD",
  passwordBody:
    "Change it with a code we email you. You will be signed out on every device.",
  passwordAction: "CHANGE PASSWORD",
  emailTitle: "EMAIL",
  emailBody:
    "Needs a code from your current address and one from the new address.",
  emailAction: "CHANGE EMAIL",
  sessionsTitle: "SESSIONS",
  sessionsBody:
    "Sign out of every device where you are logged in, including this one.",
  sessionsAction: "LOG OUT EVERYWHERE",
  deleteTitle: "DELETE ACCOUNT",
  deleteBody:
    "Your profile is hidden right away and removed after 30 days. Sign in during that time to cancel.",
  deleteAction: "DELETE ACCOUNT",
} as const;
