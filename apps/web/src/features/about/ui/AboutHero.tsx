// File : /team-samsara/apps/web/src/features/about/ui/AboutHero.tsx
// Version : 1.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : The top of the About page: the tagline, the big "What is Team Samsara" title, one
// line about the team and a wide picture, on the dark band.

import { aboutHero } from "../data/aboutContent";
import { AboutPageText } from "../view/aboutPageContent";
import { AboutMedia } from "./AboutMedia";
import layout from "./aboutLayout.module.css";
import styles from "./AboutHero.module.css";

export function AboutHero() {
  return (
    <section className={styles.hero}>
      <div className={`${layout.wrap} ${styles.copy}`}>
        <p className={styles.tagline}>{aboutHero.tagline}</p>
        <h1 className={styles.title}>
          {AboutPageText.titleLead}
          <span className={styles.name}>{AboutPageText.titleName}</span>
        </h1>
        <p className={styles.intro}>{aboutHero.intro}</p>
      </div>
      <div className={layout.wrap}>
        <AboutMedia media={aboutHero.media} className={styles.media} />
      </div>
    </section>
  );
}
