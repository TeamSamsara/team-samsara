// File : /team-samsara/apps/web/src/lib/devInvariants.ts
// Version : 1.0.0
// Latest commit: feature/string-magic-value-conventions
// Author : Gerrah
// Purpose : Developer-facing invariant/assertion messages - text only a developer debugging
// a broken setup would ever see, never a real user. Kept separate from uiMessages.ts since
// the two categories have different owners and different audiences, even though both are
// semantic strings under the same magic-value convention.

export const DevInvariants = {
  useAuthOutsideProvider: "useAuth must be used within an AuthProvider",
} as const;
