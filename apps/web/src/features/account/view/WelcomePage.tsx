// File : /team-samsara/apps/web/src/features/account/view/WelcomePage.tsx
// Version : 1.1.0
// Latest commit: task/web-auth-onboarding-navbar
// Author : Gerrah
// Purpose : The page a new member lands on after confirming their email: a
// short welcome, then the skippable "Create your profile" step. A member who
// has already finished onboarding is sent to the home page instead.

import { useState } from "react";
import { Navigate, useNavigate } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import { useAuth } from "@/app/useAuth";
import { Button } from "@/shared/components/Button/Button";
import { routePaths } from "@/shared/constants/routePaths";
import { myProfileQueryKey, useMyProfile } from "../hooks/useMyProfile";
import { OnboardingLayout } from "../ui/OnboardingLayout";
import { ProfileStep } from "./ProfileStep";
import styles from "./WelcomePage.module.css";
import { WelcomeStep } from "./WelcomeStep";
import { ProfileStepText, WelcomeText } from "./welcomePageContent";

type Step = "welcome" | "profile";

export function WelcomePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { userId } = useAuth();
  const { data: profile, isPending, isError, refetch } = useMyProfile();
  const [step, setStep] = useState<Step>("welcome");

  // Both steps end here, after a save or a skip. The navbar avatar reads the
  // same profile, so it is refreshed before leaving.
  function finish() {
    void queryClient.invalidateQueries({ queryKey: myProfileQueryKey(userId) });
    void navigate(routePaths.home, { replace: true });
  }

  if (isPending) {
    return null;
  }

  if (profile?.onboardingCompleted) {
    return <Navigate to={routePaths.home} replace />;
  }

  const onProfileStep = step === "profile" && profile !== undefined;
  const text = onProfileStep ? ProfileStepText : WelcomeText;

  return (
    <OnboardingLayout
      headlineLines={text.headlineLines}
      subtitle={text.subtitle}
    >
      {isError || profile === undefined ? (
        <div className={styles.step}>
          <p className={styles.body}>{WelcomeText.loadFailed}</p>
          <Button onClick={() => void refetch()}>{WelcomeText.retry}</Button>
        </div>
      ) : onProfileStep ? (
        <ProfileStep profile={profile} onFinished={finish} />
      ) : (
        <WelcomeStep onStart={() => setStep("profile")} onFinished={finish} />
      )}
    </OnboardingLayout>
  );
}
