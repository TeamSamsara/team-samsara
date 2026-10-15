// File : /team-samsara/apps/web/src/shared/components/RouteGuard/RouteGuard.tsx
// Version : 2.0.0
// Latest commit: task/web-auth-foundation
// Author : Gerrah
// Purpose : One generic guard used by every protected route tree. It reads the resolved
// auth status and sends each visitor to the right place, or shows a retry message when
// the server cannot be reached.

import { useState, type ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { useAuth } from "@/app/useAuth";
import { type AccessLevel } from "@/app/AuthContext";
import { routePaths } from "@/shared/constants/routePaths";
import styles from "./RouteGuard.module.css";
import { routeGuardContent } from "./routeGuardContent";

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
        <Navigate to={routePaths.home} replace />
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
    <section className={styles.unavailable}>
      <h1 className={styles.title}>{routeGuardContent.unavailableTitle}</h1>
      <p className={styles.body}>{routeGuardContent.unavailableBody}</p>
      <button
        type="button"
        className={styles.retry}
        disabled={isRetrying}
        onClick={() => void retry()}
      >
        {routeGuardContent.retryLabel}
      </button>
    </section>
  );
}
