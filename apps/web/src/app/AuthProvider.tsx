// File : /team-samsara/apps/web/src/app/AuthProvider.tsx
// Version : 1.1.0
// Latest commit: feature/apps-web-foundation
// Author : Gerrah
// Purpose : Subscribes to Firebase's auth state and populates AuthContext
// (app/AuthContext.ts). Mirrors the backend's ICurrentUserContext - one
// source of truth for "who's signed in" on the frontend. AccessLevel comes
// from the "accessLevel" custom claim on the user's ID token; defaults to
// Guest when absent, same as the backend. Exports only this component -
// the context and useAuth hook live in their own files.

import { useEffect, useState, type ReactNode } from "react";
import { onAuthStateChanged, type User } from "firebase/auth";
import { auth } from "@/lib/firebase";
import { AuthContext, type AccessLevel } from "./AuthContext";

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [accessLevel, setAccessLevel] = useState<AccessLevel>("Guest");
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const unsubscribe = onAuthStateChanged(auth, async (currentUser) => {
      setUser(currentUser);

      if (currentUser) {
        const tokenResult = await currentUser.getIdTokenResult();
        const claimValue = tokenResult.claims.accessLevel;
        setAccessLevel(
          claimValue === "Member" || claimValue === "Admin"
            ? claimValue
            : "Guest",
        );
      } else {
        setAccessLevel("Guest");
      }

      setIsLoading(false);
    });

    return unsubscribe;
  }, []);

  return (
    <AuthContext.Provider value={{ user, accessLevel, isLoading }}>
      {children}
    </AuthContext.Provider>
  );
}
