// File : /team-samsara/apps/web/src/app/AuthContext.ts
// Version : 2.0.0
// Latest commit: task/web-auth-foundation
// Author : Gerrah
// Purpose : The auth context and its value shape, shared by AuthProvider
// (which creates/populates it) and useAuth (which reads it). Split into its
// own file because a file exporting both a component and a non-component
// value breaks Fast Refresh (react-refresh/only-export-components).

import { createContext } from "react";
import type { User } from "firebase/auth";
import type { AccessLevel, AuthState } from "@/features/auth/types/authTypes";

export type { AccessLevel };

// What the app currently knows about the caller. The API decides every value except
// "loading" (first answer pending) and "unavailable" (the API cannot be reached, or
// answered something this app does not recognize).
export type AuthStatus = AuthState | "loading" | "unavailable";

export interface AuthContextValue {
  readonly user: User | null;
  readonly userId: string | null;
  readonly status: AuthStatus;
  /** "guest" until the API confirms a member; never taken from the token. */
  readonly accessLevel: AccessLevel;
  /** Asks the API again who the caller is; the one refresh screens call after a step. */
  readonly refreshAccount: () => Promise<AuthStatus>;
  /** Ends the session on the server (best effort), then signs out of Firebase. */
  readonly signOut: () => Promise<void>;
}

export const AuthContext = createContext<AuthContextValue | undefined>(
  undefined,
);
