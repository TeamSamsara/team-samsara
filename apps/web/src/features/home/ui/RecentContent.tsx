// File : /team-samsara/apps/web/src/features/home/ui/RecentContent.tsx
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The "Recent content" row near the bottom of the home page: a
// heading and three tiles of equal size. The tiles come from
// data/homeTiles.ts and are drawn by HomeTileCard.

import { recentContent } from "../data/homeTiles";
import { HomeTileCard } from "./HomeTileCard";
import { HomeTilesText } from "./homeTilesContent";
import styles from "./RecentContent.module.css";

export function RecentContent() {
  return (
    <section className={styles.section}>
      <h2 className={styles.heading}>{HomeTilesText.recentContentHeading}</h2>
      <ul className={styles.list}>
        {recentContent.map((tile) => (
          <HomeTileCard key={tile.id} tile={tile} className={styles.tile} />
        ))}
      </ul>
    </section>
  );
}
