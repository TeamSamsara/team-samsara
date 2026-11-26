// File : /team-samsara/apps/web/src/features/teams/data/teams.ts
// Version : 1.0.1
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : The teams and their rosters. This file stands in for the CMS until it is built: the
// CMS will later return the same Team shape, so the pages will not need to change. Photos,
// real names and social links are filled in as they are collected.

import type { Team, TeamMember } from "../types/teamTypes";

const leagueOfLegends = "LEAGUE OF LEGENDS";

function player(id: string, handle: string): TeamMember {
  return { id, handle, role: "player" };
}

export const teams: readonly Team[] = [
  {
    slug: "team-samsara",
    name: "TEAM SAMSARA",
    game: leagueOfLegends,
    members: [
      player("kappa", "Kappa"),
      player("toothbooth", "Toothbooth"),
      player("shimmer", "Shimmer"),
      player("broadcoma", "Broadcoma"),
      player("destiny", "Destiny"),
      { id: "exorant", handle: "Exorant", role: "headCoach" },
    ],
  },
  {
    slug: "team-samsara-bloom",
    name: "TEAM SAMSARA BLOOM",
    game: leagueOfLegends,
    members: [
      player("fliptik", "Fliptik"),
      player("incite", "Incite"),
      player("worker-cops", "Worker cops"),
      player("okami", "Okami"),
      player("david", "David"),
      player("shihai", "Shihai"),
      player("salty", "Salty"),
      player("neversaw", "Neversaw"),
      { id: "dorwee", handle: "Dorwee", role: "headCoach" },
      { id: "seaborg", handle: "Seaborg", role: "assistantCoach" },
    ],
  },
];

export function findTeam(slug: string | undefined): Team | undefined {
  return teams.find((team) => team.slug === slug);
}
