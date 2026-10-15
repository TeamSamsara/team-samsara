// File : /team-samsara/apps/web/src/features/home/ui/homeTilesContent.ts
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The fixed text of the home page tile sections (Recent Updates and
// Recent Content). The tiles themselves come from data/homeTiles.ts.

export const HomeTilesText = {
  recentUpdatesLabel: "Recent updates",
  recentContentHeading: "Recent content",
  /** What screen readers announce for a tile that is a link. */
  tileLinkLabel: (kind: string, title: string) => `${kind}: ${title}`,
} as const;
