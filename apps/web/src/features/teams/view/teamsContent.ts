// File : /team-samsara/apps/web/src/features/teams/view/teamsContent.ts
// Version : 1.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : The words of the teams pages: the headings, the role names and the labels read by
// screen readers.

import type { TeamRole } from "../types/teamTypes";

export const TeamsText = {
  overviewTitle: "MEET OUR TEAMS",
  overviewList: "Teams",
  players: "PLAYERS",
  staff: "STAFF",
  openTeam: (name: string) => `Open ${name}`,
  openMember: (handle: string) => `Show the card of ${handle}`,
  closeCard: "Close",
  memberLinks: (handle: string) => `${handle} online`,
  noPhoto: "No photo yet",
} as const;

export const TeamRoleLabels: Readonly<Record<TeamRole, string>> = {
  player: "PLAYER",
  headCoach: "HEAD COACH",
  assistantCoach: "ASSISTANT COACH",
};

export const SocialLabels = {
  twitch: "Twitch",
  x: "X",
  instagram: "Instagram",
} as const;
