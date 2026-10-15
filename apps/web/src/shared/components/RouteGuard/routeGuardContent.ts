// File : /team-samsara/apps/web/src/shared/components/RouteGuard/routeGuardContent.ts
// Version : 1.0.0
// Latest commit: task/web-auth-foundation
// Author : Gerrah
// Purpose : The text shown when a protected page cannot check who the visitor is.

export const routeGuardContent = {
  unavailableTitle: "We can't reach the server",
  unavailableBody:
    "Check your connection and try again. Public pages are still available.",
  retryLabel: "Try again",
} as const;
