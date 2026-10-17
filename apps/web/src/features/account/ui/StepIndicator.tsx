// File : /team-samsara/apps/web/src/features/account/ui/StepIndicator.tsx
// Version : 1.0.0
// Latest commit: task/web-auth-onboarding-navbar
// Author : Gerrah
// Purpose : The bars and "STEP 1 OF 2" label above an onboarding step.

import styles from "./StepIndicator.module.css";

interface StepIndicatorProps {
  /** The step the member is on, starting at 1. */
  current: number;
  /** How many steps there are. */
  total: number;
  /** The text next to the bars, for example "STEP 1 OF 2". */
  label: string;
}

export function StepIndicator({ current, total, label }: StepIndicatorProps) {
  return (
    <div className={styles.indicator}>
      <div className={styles.bars} aria-hidden="true">
        {Array.from({ length: total }, (_, index) => (
          <span
            key={index}
            className={styles.bar}
            data-done={index < current}
          />
        ))}
      </div>
      <p className={styles.label}>{label}</p>
    </div>
  );
}
