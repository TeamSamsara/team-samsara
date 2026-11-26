// File : /team-samsara/apps/web/src/features/account/hooks/useSkipOnboarding.ts
// Version : 1.1.0
// Latest commit: task/web-cleanup-and-glow
// Author : Gerrah
// Purpose : The "Skip for now" action shared by both steps: marks onboarding
// as done without changing the profile, then continues. A failure stays on the
// screen with a message.

import { useState } from "react";
import { skipOnboarding } from "../service/onboardingService";
import { onboardingFailureMessage } from "../view/welcomePageContent";

export function useSkipOnboarding(onDone: () => void) {
  const [isSkipping, setIsSkipping] = useState(false);
  const [skipError, setSkipError] = useState<string | null>(null);

  async function skip() {
    if (isSkipping) {
      return;
    }

    setIsSkipping(true);
    setSkipError(null);
    const attempt = await skipOnboarding();

    if (attempt.kind === "saved") {
      onDone();
      return;
    }

    setIsSkipping(false);
    setSkipError(onboardingFailureMessage(attempt));
  }

  return { skip, isSkipping, skipError };
}
