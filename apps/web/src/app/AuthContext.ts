// File : /team-samsara/apps/web/src/app/AuthContext.ts
// Version : 1.0.0
// Latest commit: feature/apps-web-foundation
// Author : Gerrah
// Purpose : The auth context and its value shape, shared by AuthProvider
// (which creates/populates it) and useAuth (which reads it). Split into its
// own file because a file exporting both a component and a non-component
// value breaks Fast Refresh (react-refresh/only-export-components).

import { createContext } from "react";
import type { User } from "firebase/auth";

export type AccessLevel = "Guest" | "Member" | "Admin";

export interface AuthContextValue {
  user: User | null;
  accessLevel: AccessLevel;
  isLoading: boolean;
}

export const AuthContext = createContext<AuthContextValue | undefined>(
  undefined,
);
