// File : /team-samsara/apps/web/src/features/about/ui/CallToActionSection.tsx
// Version : 1.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : The green band at the bottom of the About page: an invitation to be part of what
// comes next, with a link to the contact page and a link to the teams.

import { Link } from "react-router-dom";
import { routePaths } from "@/shared/constants/routePaths";
import { aboutCallToAction } from "../data/aboutContent";
import { AboutPageText } from "../view/aboutPageContent";
import layout from "./aboutLayout.module.css";
import styles from "./CallToActionSection.module.css";

export function CallToActionSection() {
  return (
    <section className={styles.band} aria-labelledby="about-cta">
      <div className={`${layout.wrap} ${styles.content}`}>
        <h2 id="about-cta" className={styles.title}>
          {aboutCallToAction.title}
        </h2>
        <p className={styles.body}>{aboutCallToAction.body}</p>
        <div className={styles.actions}>
          <Link to={routePaths.contact} className={styles.button}>
            {AboutPageText.contactCta}
          </Link>
          <Link to={routePaths.teams} className={styles.button}>
            {AboutPageText.teamsCta}
          </Link>
        </div>
      </div>
    </section>
  );
}
