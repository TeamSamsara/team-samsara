// File : /team-samsara/apps/web/src/features/about/ui/BrandSection.tsx
// Version : 1.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : "Our brand": what the name means, then one block for each word of the cycle
// (Grow, Evolve, Renew), each a headline, a text and a picture, alternating sides.

import { aboutBrandIntro, aboutBrandValues } from "../data/aboutContent";
import { SplitSection } from "./SplitSection";
import layout from "./aboutLayout.module.css";
import styles from "./BrandSection.module.css";

export function BrandSection() {
  return (
    <div className={styles.brand}>
      <section
        className={`${layout.wrap} ${styles.intro}`}
        aria-labelledby="about-brand"
      >
        <h2 id="about-brand" className={layout.heading}>
          {aboutBrandIntro.title}
        </h2>
        <p className={layout.paragraph}>{aboutBrandIntro.body}</p>
      </section>

      {aboutBrandValues.map((value, index) => (
        <SplitSection key={value.id} block={value} reversed={index % 2 === 1} />
      ))}
    </div>
  );
}
