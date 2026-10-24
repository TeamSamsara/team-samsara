// File : /team-samsara/apps/web/src/app/AuthProvider.tsx
// Version : 2.1.0
// Latest commit: task/web-notifications
// Author : Gerrah
// Purpose : Owns the resolved auth state. Firebase says who signed in; GET /account/me says
// what they may do. A generation counter keeps a slow answer from overwriting a newer one.
// It also keeps the navbar's "last visit was a member" note in step with each answer.

import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import {
  onAuthStateChanged,
  signOut as firebaseSignOut,
  type User,
} from "firebase/auth";
import { getAccount, logOut } from "@/features/auth/service/accountService";
import type { AccessLevel } from "@/features/auth/types/authTypes";
import { clearAuthHint, ensureAuthHint } from "@/lib/authHint";
import { auth } from "@/lib/firebase";
import {
  AuthContext,
  type AuthContextValue,
  type AuthStatus,
} from "./AuthContext";

interface ResolvedAccount {
  readonly status: AuthStatus;
  readonly userId: string | null;
  readonly accessLevel: AccessLevel;
  /** The API refused the token itself, so the Firebase session is no longer usable. */
  readonly tokenRejected: boolean;
}

const guest = {
  userId: null,
  accessLevel: "guest",
  tokenRejected: false,
} as const;

const loadingAccount: ResolvedAccount = { ...guest, status: "loading" };
const anonymousAccount: ResolvedAccount = { ...guest, status: "anonymous" };
const unavailableAccount: ResolvedAccount = { ...guest, status: "unavailable" };

// Asks the API what the signed-in user is. Nothing here is read from the token.
async function resolveAccount(user: User | null): Promise<ResolvedAccount> {
  if (user === null) {
    return anonymousAccount;
  }

  const outcome = await getAccount();

  switch (outcome.kind) {
    case "answer": {
      const { authState, accessLevel, userId } = outcome.value;
      return {
        status: authState,
        userId,
        // Only a verified member carries a level above guest.
        accessLevel: authState === "member" ? accessLevel : "guest",
        tokenRejected: false,
      };
    }
    case "unauthorized":
      return { ...anonymousAccount, tokenRejected: true };
    case "forbidden":
      return outcome.authState === null
        ? unavailableAccount
        : { ...guest, status: outcome.authState };
    default:
      return unavailableAccount;
  }
}

// Keeps the navbar's note in step with the API's answer. "unavailable" means the
// call failed rather than answered, so it leaves the note as it was.
function syncAuthHint(status: AuthStatus): void {
  if (status === "member") {
    ensureAuthHint();
    return;
  }

  if (status !== "unavailable") {
    clearAuthHint();
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [account, setAccount] = useState<ResolvedAccount>(loadingAccount);

  // Each resolution takes a number; only the newest may change state.
  const generation = useRef(0);
  const currentStatus = useRef<AuthStatus>("loading");

  const resolve = useCallback(
    async (nextUser: User | null): Promise<AuthStatus> => {
      const ticket = ++generation.current;
      const resolved = await resolveAccount(nextUser);

      if (ticket !== generation.current) {
        return currentStatus.current;
      }

      currentStatus.current = resolved.status;
      syncAuthHint(resolved.status);
      setUser(resolved.tokenRejected ? null : nextUser);
      setAccount(resolved);

      if (resolved.tokenRejected) {
        await firebaseSignOut(auth);
      }

      return resolved.status;
    },
    [],
  );

  useEffect(() => {
    return onAuthStateChanged(auth, (firebaseUser) => {
      void resolve(firebaseUser);
    });
  }, [resolve]);

  const refreshAccount = useCallback(
    () => resolve(auth.currentUser),
    [resolve],
  );

  const signOut = useCallback(async () => {
    clearAuthHint();
    // Best effort: a failed server call must not keep the person signed in here.
    await logOut();
    await firebaseSignOut(auth);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      userId: account.userId,
      status: account.status,
      accessLevel: account.accessLevel,
      refreshAccount,
      signOut,
    }),
    [user, account, refreshAccount, signOut],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
