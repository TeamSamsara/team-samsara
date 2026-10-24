// File : /team-samsara/apps/web/src/features/auth/types/codeScreenState.ts
// Version : 1.0.0
// Latest commit: task/web-auth-screens
// Author : Gerrah

export interface CodeScreenState {
  readonly codeLength: number;
  readonly retryAfterSeconds: number;
}

export function readCodeScreenState(value: unknown): CodeScreenState | null {
  if (typeof value !== "object" || value === null) {
    return null;
  }

  const { codeLength, retryAfterSeconds } = value as Record<string, unknown>;

  if (
    typeof codeLength !== "number" ||
    !Number.isInteger(codeLength) ||
    codeLength <= 0 ||
    typeof retryAfterSeconds !== "number" ||
    !Number.isInteger(retryAfterSeconds) ||
    retryAfterSeconds < 0
  ) {
    return null;
  }

  return { codeLength, retryAfterSeconds };
}
