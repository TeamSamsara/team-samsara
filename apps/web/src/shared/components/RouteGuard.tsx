// File : /team-samsara/apps/web/src/shared/components/RouteGuard.tsx
// Version : 1.0.1
// Latest commit: feature/apps-web-foundation
// Author : Gerrah
// Purpose : One generic guard used by every protected route tree.
// Redirects to /login if unauthenticated, or if the current user's
// AccessLevel is below the required minimum. AccessLevel now imported from
// AuthContext.ts (single source of truth) rather than redeclared here.

import type { ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { useAuth } from "@/app/useAuth";
import { type AccessLevel } from "@/app/AuthContext";
import { routePaths } from "@/shared/constants/routePaths";

const accessLevelRank: Record<AccessLevel, number> = {
  Guest: 0,
  Member: 1,
  Admin: 2,
};

interface RouteGuardProps {
  requiredAccessLevel: AccessLevel;
  children: ReactNode;
}

export function RouteGuard({ requiredAccessLevel, children }: RouteGuardProps) {
  const { user, accessLevel, isLoading } = useAuth();

  if (isLoading) {
    return null;
  }

  const isAuthorized =
    user !== null &&
    accessLevelRank[accessLevel] >= accessLevelRank[requiredAccessLevel];

  if (!isAuthorized) {
    return <Navigate to={routePaths.login} replace />;
  }

  return <>{children}</>;
}
