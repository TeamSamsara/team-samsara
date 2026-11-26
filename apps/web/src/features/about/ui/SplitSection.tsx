// File : /team-samsara/apps/web/src/features/about/ui/SplitSection.tsx
// Version : 1.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : A headline with its paragraphs beside a picture. Used for the mission, the energy
// behind the team, the company and each of the brand words; `reversed` puts the text first on a
// wide screen so neighbouring blocks alternate. On a narrow screen the picture always comes first.

import type { AboutBlock } from "../data/aboutContent";
import { AboutMedia } from "./AboutMedia";
import layout from "./aboutLayout.module.css";
import styles from "./SplitSection.module.css";

interface SplitSectionProps {
  readonly block: AboutBlock;
  readonly reversed?: boolean;
}

export function SplitSection({ block, reversed = false }: SplitSectionProps) {
  const headingId = `about-${block.id}`;

  return (
    <section
      className={`${layout.wrap} ${styles.split}`}
      data-reversed={reversed}
      aria-labelledby={headingId}
    >
      <AboutMedia media={block.media} className={styles.picture} />
      <div className={styles.copy}>
        <h2 id={headingId} className={layout.heading}>
          {block.title}
        </h2>
        {block.paragraphs.map((paragraph) => (
          <p key={paragraph} className={layout.paragraph}>
            {paragraph}
          </p>
        ))}
      </div>
    </section>
  );
}
