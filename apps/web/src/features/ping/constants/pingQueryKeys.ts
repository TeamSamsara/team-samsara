// File : /team-samsara/apps/web/src/features/ping/constants/pingQueryKeys.ts
// Version : 1.0.0
// Latest commit: feature/apps-web-foundation
// Author : Gerrah
// Purpose : Centralizes the ping feature's TanStack Query keys, so a rename
// or an invalidation call only needs to change in one place.

export const pingQueryKeys = {
  ping: ["ping"] as const,
};
