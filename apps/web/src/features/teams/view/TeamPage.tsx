// File : /team-samsara/apps/web/src/features/teams/view/TeamPage.tsx
// Version : 1.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : The roster page of one team (/teams/:teamSlug): a banner with the team's name and
// game, the players, then the staff. A person's card opens over the page. An address that
// names no team shows the not-found page.

import { useState } from "react";
import { useParams } from "react-router-dom";
import { NotFoundPage } from "@/shared/components/StatusPage/NotFoundPage";
import { findTeam } from "../data/teams";
import { MemberCard } from "../ui/MemberCard";
import { MemberModal } from "../ui/MemberModal";
import { PageBanner } from "../ui/PageBanner";
import type { Team, TeamMember } from "../types/teamTypes";
import { TeamsText } from "./teamsContent";
import styles from "./TeamPage.module.css";

interface RosterProps {
  readonly heading: string;
  readonly members: readonly TeamMember[];
  readonly onOpen: (member: TeamMember) => void;
}

function Roster({ heading, members, onOpen }: RosterProps) {
  if (members.length === 0) {
    return null;
  }

  return (
    <section className={styles.roster}>
      <h2 className={styles.heading}>{heading}</h2>
      <ul className={styles.cards}>
        {members.map((member) => (
          <li key={member.id}>
            <MemberCard member={member} onOpen={onOpen} />
          </li>
        ))}
      </ul>
    </section>
  );
}

function TeamRoster({ team }: { readonly team: Team }) {
  const [selected, setSelected] = useState<TeamMember | null>(null);
  const players = team.members.filter((member) => member.role === "player");
  const staff = team.members.filter((member) => member.role !== "player");

  return (
    <main>
      <PageBanner
        title={team.name}
        subtitle={team.game}
        assetId={team.bannerAssetId}
      />

      <div className={styles.rosters}>
        <Roster
          heading={TeamsText.players}
          members={players}
          onOpen={setSelected}
        />
        <Roster
          heading={TeamsText.staff}
          members={staff}
          onOpen={setSelected}
        />
      </div>

      {selected ? (
        <MemberModal member={selected} onClose={() => setSelected(null)} />
      ) : null}
    </main>
  );
}

export function TeamPage() {
  const { teamSlug } = useParams();
  const team = findTeam(teamSlug);

  return team ? <TeamRoster key={team.slug} team={team} /> : <NotFoundPage />;
}
