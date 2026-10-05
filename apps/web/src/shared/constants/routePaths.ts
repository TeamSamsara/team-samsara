// File : /team-samsara/apps/web/src/shared/constants/routePaths.ts
// Version : 1.0.0
// Latest commit: feature/apps-web-foundation
// Author : Gerrah
// Purpose : Central source of truth for every route path in the app. No
// component should hardcode a path string directly — always import from here.

export const routePaths = {
  home: "/",
  ping: "/ping",
  login: "/login",
  account: "/account",
  cms: "/cms",
} as const;
