// File : /team-samsara/apps/web/src/lib/uiMessages.ts
// Version : 1.0.0
// Latest commit: feature/string-magic-value-conventions
// Author : Gerrah
// Purpose : User-facing fallback text, currently duplicated across ErrorBoundary.tsx and
// apiClient.ts. Mirrors the backend's ResultMessages.cs; the "unexpected error" wording is
// deliberately kept matching between the two languages, though nothing enforces that beyond
// this comment - a literal can't be shared across the boundary.

export const UiMessages = {
  unexpectedError: "An unexpected error occurred.",
  unknownError: "Unknown error",
} as const;
