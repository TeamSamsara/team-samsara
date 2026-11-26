// File : /team-samsara/apps/web/src/features/about/ui/StorySection.tsx
// Version : 1.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : "Our story": the founding goal as one big headline with its last words
// highlighted, then a row of milestone cards (2025, the DVM Tour, the climb to the LCS).

import { aboutStory } from "../data/aboutContent";
import { AboutMedia } from "./AboutMedia";
import layout from "./aboutLayout.module.css";
import styles from "./StorySection.module.css";

export function StorySection() {
  return (
    <section className={`${layout.wrap} ${styles.story}`}>
      <h2 className={styles.headline}>
        {aboutStory.headline}{" "}
        <span className={styles.highlight}>{aboutStory.highlight}</span>
      </h2>

      <ol className={styles.milestones}>
        {aboutStory.milestones.map((milestone) => (
          <li key={milestone.id} className={styles.milestone}>
            <AboutMedia
              media={milestone.media}
              className={`${layout.cut} ${styles.picture}`}
            />
            <p className={styles.label}>{milestone.label}</p>
            <h3 className={styles.title}>{milestone.title}</h3>
            <p className={styles.body}>{milestone.body}</p>
          </li>
        ))}
      </ol>
    </section>
  );
}
