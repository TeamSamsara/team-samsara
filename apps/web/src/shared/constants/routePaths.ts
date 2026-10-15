// File : /team-samsara/apps/web/src/shared/constants/routePaths.ts
// Version : 1.2.0
// Latest commit: task/web-auth-foundation
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
  members: "/members",
  cms: "/cms",
} as const;

// Public profile address for a member id, e.g. /members/abc123
export function memberPath(memberId: string): string {
  return `${routePaths.members}/${encodeURIComponent(memberId)}`;
}
