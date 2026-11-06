// File : /team-samsara/apps/web/src/shared/constants/routePaths.ts
// Version : 1.5.0
// Latest commit: task/web-member-profile
// Author : Gerrah
// Purpose : Central source of truth for every route path in the app. No
// component should hardcode a path string directly — always import from here.

export const routePaths = {
  home: "/",
  ping: "/ping",
  login: "/login",
  loginVerify: "/login/verify",
  register: "/register",
  registerVerify: "/register/verify",
  forgotPassword: "/forgot-password",
  welcome: "/welcome",
  teams: "/teams",
  about: "/about",
  contact: "/contact",
  account: "/account",
  accountProfile: "/account/profile",
  accountSettings: "/account/settings",
  accountSecurity: "/account/security",
  accountSecurityPassword: "/account/security/password",
  accountSecurityEmail: "/account/security/email",
  members: "/members",
  memberProfile: "/members/:handle",
  cms: "/cms",
} as const;

// Profile address for a member handle, e.g. /members/gerrah
export function memberPath(handle: string): string {
  return `${routePaths.members}/${encodeURIComponent(handle)}`;
}
