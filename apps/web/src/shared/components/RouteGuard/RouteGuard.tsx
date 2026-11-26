// File : /team-samsara/apps/web/src/shared/components/RouteGuard/RouteGuard.tsx
// Version : 3.0.0
// Latest commit: task/web-error-pages
// Author : Gerrah
// Purpose : One generic guard used by every protected route tree. It sends each visitor
// to the right place, or shows the 500 or 403 page.

import { useState, type ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { useAuth } from "@/app/useAuth";
import { type AccessLevel } from "@/app/AuthContext";
import { ForbiddenPage } from "@/shared/components/StatusPage/ForbiddenPage";
import { ServerErrorPage } from "@/shared/components/StatusPage/ServerErrorPage";
import { routePaths } from "@/shared/constants/routePaths";

const accessLevelRank: Record<AccessLevel, number> = {
  guest: 0,
  member: 1,
  admin: 2,
};

interface RouteGuardProps {
  requiredAccessLevel: AccessLevel;
  children: ReactNode;
}

export function RouteGuard({ requiredAccessLevel, children }: RouteGuardProps) {
  const { status, accessLevel } = useAuth();

  switch (status) {
    case "loading":
      return null;
    case "unavailable":
      return <Unavailable />;
    case "anonymous":
      return <Navigate to={routePaths.login} replace />;
    case "registrationIncomplete":
      return <Navigate to={routePaths.registerVerify} replace />;
    case "verificationRequired":
      return <Navigate to={routePaths.loginVerify} replace />;
    case "member":
      return accessLevelRank[accessLevel] >=
        accessLevelRank[requiredAccessLevel] ? (
        <>{children}</>
      ) : (
        <ForbiddenPage />
      );
  }
}

function Unavailable() {
  const { refreshAccount } = useAuth();
  const [isRetrying, setIsRetrying] = useState(false);

  async function retry() {
    setIsRetrying(true);
    await refreshAccount();
    setIsRetrying(false);
  }

  return (
    <ServerErrorPage isRetrying={isRetrying} onRetry={() => void retry()} />
  );
}
