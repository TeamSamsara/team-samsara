// File : /team-samsara/apps/web/src/features/auth/service/firebaseAuthService.ts
// Version : 1.0.0
// Latest commit: task/web-auth-screens
// Author : Gerrah
// Purpose : Creates and signs in Firebase email and password accounts. Firebase's error
// codes become a short list of failures the screens can choose a message for.

import { FirebaseError } from "firebase/app";
import {
  createUserWithEmailAndPassword,
  signInWithEmailAndPassword,
} from "firebase/auth";
import { auth } from "@/lib/firebase";

export type FirebaseFailure =
  | "emailInUse"
  | "invalidEmail"
  | "weakPassword"
  | "invalidCredentials"
  | "accountDisabled"
  | "tooManyAttempts"
  | "network"
  | "unknown";

export type FirebaseResult =
  | { readonly ok: true }
  | { readonly ok: false; readonly failure: FirebaseFailure };

// An unknown email and a wrong password share one failure, so the answer never says which.
const failureByCode: Partial<Record<string, FirebaseFailure>> = {
  "auth/email-already-in-use": "emailInUse",
  "auth/invalid-email": "invalidEmail",
  "auth/weak-password": "weakPassword",
  "auth/invalid-credential": "invalidCredentials",
  "auth/invalid-login-credentials": "invalidCredentials",
  "auth/wrong-password": "invalidCredentials",
  "auth/user-not-found": "invalidCredentials",
  "auth/user-disabled": "accountDisabled",
  "auth/too-many-requests": "tooManyAttempts",
  "auth/network-request-failed": "network",
};

function toFailure(error: unknown): FirebaseFailure {
  if (error instanceof FirebaseError) {
    return failureByCode[error.code] ?? "unknown";
  }

  return "unknown";
}

async function run(action: () => Promise<unknown>): Promise<FirebaseResult> {
  try {
    await action();
    return { ok: true };
  } catch (error) {
    return { ok: false, failure: toFailure(error) };
  }
}

export function createAccount(
  email: string,
  password: string,
): Promise<FirebaseResult> {
  return run(() =>
    createUserWithEmailAndPassword(auth, email.trim(), password),
  );
}

export function signIn(
  email: string,
  password: string,
): Promise<FirebaseResult> {
  return run(() => signInWithEmailAndPassword(auth, email.trim(), password));
}
