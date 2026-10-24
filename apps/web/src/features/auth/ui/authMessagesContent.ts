// File : /team-samsara/apps/web/src/features/auth/ui/authMessagesContent.ts
// Version : 1.0.0
// Latest commit: task/web-auth-screens
// Author : Gerrah
// Purpose : Messages shared by the auth screens for problems that are not
// about one field: the connection, the server and too many attempts.

function waitText(seconds: number): string {
  if (seconds >= 60) {
    const minutes = Math.ceil(seconds / 60);
    return `${minutes} ${minutes === 1 ? "minute" : "minutes"}`;
  }

  return `${seconds} ${seconds === 1 ? "second" : "seconds"}`;
}

export const AuthMessages = {
  network: "We couldn't reach the server. Check your connection and try again.",
  unavailable:
    "Something went wrong on our side. Please try again in a moment.",
  unknown: "Something went wrong. Please try again.",
  rateLimited: (retryAfterSeconds: number | null): string =>
    retryAfterSeconds === null
      ? "Too many attempts. Please wait a moment and try again."
      : `Too many attempts. Try again in ${waitText(retryAfterSeconds)}.`,
} as const;
