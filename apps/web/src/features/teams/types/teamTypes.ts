// File : /team-samsara/apps/web/src/features/teams/types/teamTypes.ts
// Version : 1.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : The shapes of a team and the people on it. The data file fills them today; the CMS
// will return the same shapes later.

export type TeamRole = "player" | "headCoach" | "assistantCoach";

/** The accounts a person can link; each is the full address of their page. */
export type MemberSocials = {
  twitch?: string;
  x?: string;
  instagram?: string;
};

export type TeamMember = {
  /** Stable key for React lists and the card. */
  id: string;
  /** The name they go by, shown big. */
  handle: string;
  /** Their own name, shown under the handle; left out when they prefer not to share it. */
  realName?: string;
  role: TeamRole;
  socials?: MemberSocials;
  /** Asset id of the cut-out photo; leave out until it is uploaded. */
  photoAssetId?: string;
};

export type Team = {
  /** Used in the address of the team's page. */
  slug: string;
  name: string;
  /** The game the team plays, shown under the name. */
  game: string;
  /** Asset id of the banner picture; leave out until it is uploaded. */
  bannerAssetId?: string;
  members: readonly TeamMember[];
};
