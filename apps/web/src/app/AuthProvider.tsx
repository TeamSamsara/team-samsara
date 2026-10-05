// File : /team-samsara/apps/web/src/app/AuthProvider.tsx
// Version : 1.2.0
// Latest commit: feature/string-magic-value-conventions
// Author : Gerrah
// Purpose : Subscribes to Firebase auth state and populates AuthContext.

import { useEffect, useState, type ReactNode } from "react";
import { onAuthStateChanged, type User } from "firebase/auth";
import { auth } from "@/lib/firebase";
import { AuthContext, type AccessLevel } from "./AuthContext";
import { ClaimNames } from "./ClaimNames";

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [accessLevel, setAccessLevel] = useState<AccessLevel>("Guest");
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const unsubscribe = onAuthStateChanged(auth, async (currentUser) => {
      setUser(currentUser);

      if (currentUser) {
        const tokenResult = await currentUser.getIdTokenResult();
        const claimValue = tokenResult.claims[ClaimNames.accessLevel];
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
