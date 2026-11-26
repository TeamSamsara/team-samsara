// File : /team-samsara/apps/web/src/features/account/hooks/useMyProfile.ts
// Version : 1.0.0
// Latest commit: task/web-auth-onboarding-navbar
// Author : Gerrah
// Purpose : Reads the signed-in member's own profile once and shares it with
// every component that needs it (the navbar avatar today, the settings screen
// later). The cache is keyed by user id, so a different member never sees the
// previous member's profile.

import { useQuery } from "@tanstack/react-query";
import { useAuth } from "@/app/useAuth";
import { getProfile } from "../service/profileService";
import type { ProfileView } from "../types/profileTypes";

export function myProfileQueryKey(userId: string | null) {
  return ["profile", userId] as const;
}

async function loadProfile(): Promise<ProfileView> {
  const outcome = await getProfile();

  if (
    outcome.kind !== "answer" ||
    outcome.value.status !== "success" ||
    !outcome.value.profile
  ) {
    // Thrown so the query reports an error and can try again later.
    throw new Error("The profile could not be loaded.");
  }

  return outcome.value.profile;
}

export function useMyProfile() {
  const { status, userId } = useAuth();

  return useQuery({
    queryKey: myProfileQueryKey(userId),
    queryFn: loadProfile,
    enabled: status === "member" && userId !== null,
    staleTime: 5 * 60 * 1000,
  });
}
