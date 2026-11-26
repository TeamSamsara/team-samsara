// File : /team-samsara/apps/web/src/features/account/ui/OnboardingLayout.tsx
// Version : 1.0.0
// Latest commit: task/web-auth-onboarding-navbar
// Author : Gerrah
// Purpose : The dark two-column frame of the onboarding steps: changing photos
// with a headline on the left and the step on the right.

import type { ReactNode } from "react";
import { OnboardingSlideshow } from "./OnboardingSlideshow";
import styles from "./OnboardingLayout.module.css";

interface OnboardingLayoutProps {
  /** The big headline, one string per line. */
  headlineLines: readonly string[];
  /** The sentence under the headline. */
  subtitle: string;
  /** The step shown on the right. */
  children: ReactNode;
}

export function OnboardingLayout({
  headlineLines,
  subtitle,
  children,
}: OnboardingLayoutProps) {
  return (
    <div className={styles.layout}>
      <aside className={styles.art}>
        <OnboardingSlideshow />

        <div className={styles.copy}>
          <p className={styles.headline}>
            {headlineLines.map((line) => (
              <span key={line} className={styles.line}>
                {line}
              </span>
            ))}
          </p>
          <p className={styles.subtitle}>{subtitle}</p>
        </div>
      </aside>

      <main className={styles.content}>{children}</main>
    </div>
  );
}
