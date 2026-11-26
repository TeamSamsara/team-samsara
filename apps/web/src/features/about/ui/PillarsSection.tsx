// File : /team-samsara/apps/web/src/features/about/ui/PillarsSection.tsx
// Version : 2.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : "Our pillars": the four things Team Samsara does (competitive, entertainment,
// apparel, technology and development) as a grid of cards, each with a picture, a title and a
// short text. The cards come from data/aboutContent.ts.

import { aboutPillars } from "../data/aboutContent";
import { AboutPageText } from "../view/aboutPageContent";
import { AboutMedia } from "./AboutMedia";
import layout from "./aboutLayout.module.css";
import styles from "./PillarsSection.module.css";

export function PillarsSection() {
  return (
    <section className={styles.band} aria-labelledby="about-pillars">
      <div className={`${layout.wrap} ${styles.content}`}>
        <h2 id="about-pillars" className={layout.bigHeading}>
          {AboutPageText.pillars}
        </h2>

        <ul className={styles.grid}>
          {aboutPillars.map((pillar) => (
            <li key={pillar.id} className={styles.card}>
              <AboutMedia
                media={pillar.media}
                className={`${layout.cut} ${styles.picture}`}
              />
              <h3 className={styles.title}>{pillar.title}</h3>
              <p className={layout.paragraph}>{pillar.body}</p>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}
