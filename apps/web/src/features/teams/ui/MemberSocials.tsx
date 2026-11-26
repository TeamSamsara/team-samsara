// File : /team-samsara/apps/web/src/features/teams/ui/MemberSocials.tsx
// Version : 1.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : The row of social icons of a team member. Each icon links to the person's own page
// and opens in a new tab; an account they have not linked shows no icon, and a member with
// none shows nothing at all.

import { Icon } from "@/shared/icons/Icon";
import type { IconName } from "@/shared/icons/iconRegistry";
import type { MemberSocials as MemberSocialsData } from "../types/teamTypes";
import { SocialLabels, TeamsText } from "../view/teamsContent";
import styles from "./MemberSocials.module.css";

const socialOrder = [
  "twitch",
  "x",
  "instagram",
] as const satisfies readonly (keyof MemberSocialsData & IconName)[];

interface MemberSocialsProps {
  readonly handle: string;
  readonly socials?: MemberSocialsData;
}

export function MemberSocials({ handle, socials }: MemberSocialsProps) {
  const linked = socialOrder.filter((network) => socials?.[network]);

  if (linked.length === 0) {
    return null;
  }

  return (
    <ul className={styles.list} aria-label={TeamsText.memberLinks(handle)}>
      {linked.map((network) => (
        <li key={network}>
          <a
            className={styles.link}
            href={socials?.[network]}
            target="_blank"
            rel="noopener noreferrer"
            aria-label={`${handle} on ${SocialLabels[network]}`}
          >
            <Icon name={network} className={styles.icon} />
          </a>
        </li>
      ))}
    </ul>
  );
}
