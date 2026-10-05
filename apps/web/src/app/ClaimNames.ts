// File : /team-samsara/apps/web/src/app/ClaimNames.ts
// Version : 1.0.0
// Latest commit: feature/string-magic-value-conventions
// Author : Gerrah
// Purpose : Custom claim key names issued on Firebase ID tokens. Mirrors the backend's
// ClaimNames.cs in spirit - can't share one literal across the C#/TypeScript boundary,
// so each side keeps its own definition, kept in sync manually.

export const ClaimNames = {
  accessLevel: "accessLevel",
} as const;
