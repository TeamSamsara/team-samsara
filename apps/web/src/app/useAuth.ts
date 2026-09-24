// File : /team-samsara/apps/web/src/app/useAuth.ts
// Version : 1.0.1
// Latest commit: feature/string-magic-value-conventions
// Author : Gerrah
// Purpose : Reads AuthContext.

import { useContext } from "react";
import { AuthContext, type AuthContextValue } from "./AuthContext";
import { DevInvariants } from "@/lib/devInvariants";

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error(DevInvariants.useAuthOutsideProvider);
  }
  return context;
}
