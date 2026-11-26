// File : /team-samsara/apps/web/src/features/teams/ui/PageBanner.tsx
// Version : 1.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : The wide banner at the top of the teams pages: a picture with the page title in
// green on a dark box at the bottom left. Without a picture it is plain dark.

import { assetUrl } from "@/lib/assetUrl";
import styles from "./PageBanner.module.css";

interface PageBannerProps {
  readonly title: string;
  readonly subtitle?: string;
  /** Asset id of the picture; leave out until it is uploaded. */
  readonly assetId?: string;
}

export function PageBanner({ title, subtitle, assetId }: PageBannerProps) {
  return (
    <header className={styles.banner}>
      {assetId ? (
        <img className={styles.image} src={assetUrl(assetId)} alt="" />
      ) : null}
      <div className={styles.text}>
        <h1 className={styles.title}>
          <span>{title}</span>
        </h1>
        {subtitle ? <p className={styles.subtitle}>{subtitle}</p> : null}
      </div>
    </header>
  );
}
