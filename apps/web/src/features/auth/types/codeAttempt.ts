// File : /team-samsara/apps/web/src/features/auth/types/codeAttempt.ts
// Version : 1.0.0
// Latest commit: task/web-auth-codes-reset
// Author : Gerrah
// Purpose : What the code form needs to know after the person sends a code, or
// asks for a new one. The screens turn API answers into these.

export type CodeRejection =
  "invalidCode" | "codeExpired" | "tooManyAttempts" | "noPendingCode";

export type CodeAttempt =
  | { readonly kind: "accepted" }
  | { readonly kind: "wrongCode"; readonly message: string }
  | { readonly kind: "failed"; readonly message: string };

export type ResendAttempt =
  | { readonly kind: "waiting"; readonly retryAfterSeconds: number }
  | { readonly kind: "failed"; readonly message: string };
