// File : /team-samsara/apps/web/src/features/account/service/landingService.ts
// Version : 1.0.0
// Latest commit: task/web-auth-onboarding-navbar
// Author : Gerrah
// Purpose : Decides where a member lands after logging in: the welcome step
// when they have not finished or skipped it yet, the home page otherwise. A
// profile that cannot be read never blocks the login, so it falls back to home.

import { routePaths } from "@/shared/constants/routePaths";
import { getProfile } from "./profileService";

export async function pathAfterLogin(): Promise<string> {
  const outcome = await getProfile();

  if (
    outcome.kind === "answer" &&
    outcome.value.profile &&
    !outcome.value.profile.onboardingCompleted
  ) {
    return routePaths.welcome;
  }

  return routePaths.home;
}
