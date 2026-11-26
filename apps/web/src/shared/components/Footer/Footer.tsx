// File : /team-samsara/apps/web/src/shared/components/Footer/Footer.tsx
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The footer shown at the bottom of every page: columns of links on
// the left and the copyright line on the right. The columns come from
// footerContent.ts.

import { Link } from "react-router-dom";
import { FooterText, footerCurrentYear, footerSections } from "./footerContent";
import styles from "./Footer.module.css";

export function Footer() {
  return (
    <footer className={styles.footer}>
      <nav className={styles.sections} aria-label={FooterText.navigation}>
        {footerSections.map((section) => (
          <section key={section.heading} className={styles.section}>
            <h2 className={styles.heading}>{section.heading}</h2>
            <ul className={styles.links}>
              {section.links.map((link) => (
                <li key={link.to}>
                  <Link to={link.to} className={styles.link}>
                    {link.label}
                  </Link>
                </li>
              ))}
            </ul>
          </section>
        ))}
      </nav>
      <p className={styles.copyright}>
        {FooterText.copyright(footerCurrentYear)}
      </p>
    </footer>
  );
}
