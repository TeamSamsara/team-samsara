// File : /team-samsara/apps/web/src/features/teams/ui/TeamTile.tsx
// Version : 1.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : One team on the teams overview: a dark tile with the team name and its game that
// links to the team's page. A banner picture, when there is one, fills the tile.

import { Link } from "react-router-dom";
import { assetUrl } from "@/lib/assetUrl";
import { teamPath } from "@/shared/constants/routePaths";
import type { Team } from "../types/teamTypes";
import { TeamsText } from "../view/teamsContent";
import styles from "./TeamTile.module.css";

interface TeamTileProps {
  readonly team: Team;
}

export function TeamTile({ team }: TeamTileProps) {
  return (
    <Link
      to={teamPath(team.slug)}
      className={styles.tile}
      aria-label={TeamsText.openTeam(team.name)}
    >
      {team.bannerAssetId ? (
        <img
          className={styles.image}
          src={assetUrl(team.bannerAssetId)}
          alt=""
          loading="lazy"
        />
      ) : null}
      <span className={styles.name}>{team.name}</span>
      <span className={styles.game}>{team.game}</span>
    </Link>
  );
}
