// File : /team-samsara/apps/web/src/features/teams/ui/MemberCard.tsx
// Version : 1.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : One person on a roster: a dark card with their photo, role, handle and real name.
// The whole card is one button that opens the person's card; the social links sit above it so
// they stay separate links.

import type { TeamMember } from "../types/teamTypes";
import { TeamRoleLabels, TeamsText } from "../view/teamsContent";
import { MemberPhoto } from "./MemberPhoto";
import { MemberSocials } from "./MemberSocials";
import styles from "./MemberCard.module.css";

interface MemberCardProps {
  readonly member: TeamMember;
  readonly onOpen: (member: TeamMember) => void;
}

export function MemberCard({ member, onOpen }: MemberCardProps) {
  return (
    <article className={styles.card}>
      <MemberPhoto member={member} className={styles.photo} />

      <div className={styles.details}>
        <p className={styles.role}>{TeamRoleLabels[member.role]}</p>
        <h3 className={styles.handle}>
          <button
            type="button"
            className={styles.open}
            aria-label={TeamsText.openMember(member.handle)}
            onClick={() => onOpen(member)}
          >
            {member.handle}
          </button>
        </h3>
        {member.realName ? (
          <p className={styles.realName}>{member.realName}</p>
        ) : null}
        <MemberSocials handle={member.handle} socials={member.socials} />
      </div>
    </article>
  );
}
