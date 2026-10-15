// File : /team-samsara/apps/web/src/features/home/ui/RecentUpdates.tsx
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The brick grid of recent updates under the hero: four tiles in
// two rows, a wide one and a narrow one, swapping sides on the second row. The
// tiles come from data/homeTiles.ts and are drawn by HomeTileCard.

import { recentUpdates } from "../data/homeTiles";
import { HomeTileCard } from "./HomeTileCard";
import { HomeTilesText } from "./homeTilesContent";
import styles from "./RecentUpdates.module.css";

export function RecentUpdates() {
  return (
    <section
      className={styles.section}
      aria-label={HomeTilesText.recentUpdatesLabel}
    >
      <ul className={styles.grid}>
        {recentUpdates.map((tile) => (
          <HomeTileCard key={tile.id} tile={tile} className={styles.tile} />
        ))}
      </ul>
    </section>
  );
}
