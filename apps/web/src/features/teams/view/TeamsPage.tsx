// File : /team-samsara/apps/web/src/features/teams/view/TeamsPage.tsx
// Version : 1.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : The public teams overview: a banner and one tile for each team, each linking to
// the team's roster. The teams come from data/teams.ts.

import { teams } from "../data/teams";
import { PageBanner } from "../ui/PageBanner";
import { TeamTile } from "../ui/TeamTile";
import { TeamsText } from "./teamsContent";
import styles from "./TeamsPage.module.css";

export function TeamsPage() {
  return (
    <main>
      <PageBanner title={TeamsText.overviewTitle} />

      <ul className={styles.tiles} aria-label={TeamsText.overviewList}>
        {teams.map((team) => (
          <li key={team.slug}>
            <TeamTile team={team} />
          </li>
        ))}
      </ul>
    </main>
  );
}
