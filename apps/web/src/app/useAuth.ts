// File : /team-samsara/apps/web/src/app/useAuth.ts
// Version : 1.0.0
// Latest commit: feature/apps-web-foundation
// Author : Gerrah
// Purpose : Reads AuthContext (app/AuthContext.ts). Split from
// AuthProvider.tsx since a file exporting both a component and a hook
// breaks Fast Refresh (react-refresh/only-export-components).

import { useContext } from "react";
import { AuthContext, type AuthContextValue } from "./AuthContext";

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
