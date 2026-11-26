// File : /team-samsara/apps/web/src/features/account/hooks/useMemberProfile.ts
// Version : 1.0.0
// Latest commit: task/web-member-profile
// Author : Gerrah
// Purpose : Looks up another member's profile by handle. The answer is kept for a
// minute, so going back to a profile just visited does not ask the server again.
// A member's own profile is read from useMyProfile instead, so this stays off for it.

import { useQuery } from "@tanstack/react-query";
import { getPublicProfile } from "../service/profileService";

const staleTimeMs = 60 * 1000;

export function memberProfileQueryKey(handle: string) {
  return ["memberProfile", handle] as const;
}

// The handle must already be normalized (no @, lower case).
export function useMemberProfile(handle: string, enabled: boolean) {
  return useQuery({
    queryKey: memberProfileQueryKey(handle),
    queryFn: () => getPublicProfile(handle),
    enabled,
    staleTime: staleTimeMs,
  });
}
