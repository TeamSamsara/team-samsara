// File : /team-samsara/apps/web/src/features/account/view/WelcomeStep.tsx
// Version : 1.1.0
// Latest commit: task/web-auth-onboarding-navbar
// Author : Gerrah
// Purpose : The first onboarding step: a short welcome, then on to the profile
// step, or straight on with "Skip for now".

import { AuthLogo } from "@/features/auth/ui/AuthLogo";
import { Button } from "@/shared/components/Button/Button";
import { StepIndicator } from "../ui/StepIndicator";
import styles from "./WelcomePage.module.css";
import { useSkipOnboarding } from "./useSkipOnboarding";
import {
  OnboardingText,
  WelcomeText,
  onboardingStepCount,
} from "./welcomePageContent";

interface WelcomeStepProps {
  onStart: () => void;
  onFinished: () => void;
}

export function WelcomeStep({ onStart, onFinished }: WelcomeStepProps) {
  const { skip, isSkipping, skipError } = useSkipOnboarding(onFinished);

  return (
    <div className={styles.welcome}>
      <AuthLogo />

      <div className={styles.step}>
        <StepIndicator
          current={1}
          total={onboardingStepCount}
          label={OnboardingText.stepLabel(1, onboardingStepCount)}
        />
        <h1 className={styles.title}>{WelcomeText.title}</h1>
        <p className={styles.body}>{WelcomeText.body}</p>

        {skipError ? (
          <p className={styles.error} role="alert">
            {skipError}
          </p>
        ) : null}

        <Button fullWidth onClick={onStart}>
          {WelcomeText.start}
        </Button>
        <button
          type="button"
          className={styles.linkButton}
          disabled={isSkipping}
          onClick={() => void skip()}
        >
          {WelcomeText.skip}
        </button>
      </div>
    </div>
  );
}
