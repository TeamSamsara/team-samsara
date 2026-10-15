// File : /team-samsara/apps/web/src/features/home/data/homeTiles.ts
// Version : 1.0.1
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : PLACEHOLDER content for the home page tiles (Recent Updates and
// Recent Content): the team photos already in Staging with made-up titles, so
// the layout can be judged before the real content is decided. Replace the
// entries below, or later the CMS query returning the same HomeTile shape; the
// tile components will not need to change.

import { routePaths } from "@/shared/constants/routePaths";

export type HomeTile = {
  /** Stable key for React lists. */
  id: string;
  /** Asset id returned by POST /assets (Staging). */
  assetId: string;
  /** Describes the picture for screen readers. */
  alt: string;
  /** CSS object-position that keeps the important part of the picture in view. */
  focus?: string;
  /** Small kind of content shown above the title, for example "News" or "Video". */
  label: string;
  title: string;
  /** A path on this site, or a full address elsewhere (opens in a new tab). */
  href: string;
};

export const recentUpdates: readonly HomeTile[] = [
  {
    id: "update-1",
    assetId: "1745dbc1-000a-4804-a8b2-05e258f7c5a7",
    alt: "Team Samsara player in a green jersey seated at a gaming setup",
    label: "News",
    title: "Placeholder: a big announcement from the team",
    href: routePaths.teams,
  },
  {
    id: "update-2",
    assetId: "72673d9a-81c3-4316-a749-b03aeb10bbac",
    focus: "50% 10%",
    alt: "Smiling Team Samsara player in a green jersey making a peace sign",
    label: "Player",
    title: "Placeholder: meet the roster",
    href: routePaths.about,
  },
  {
    id: "update-3",
    assetId: "dd9cc189-3410-4bcd-b713-8ff80a2af53e",
    focus: "50% 20%",
    alt: "Two Team Samsara players in green jerseys posing on a rooftop",
    label: "Photos",
    title: "Placeholder: behind the scenes",
    href: routePaths.contact,
  },
  {
    id: "update-4",
    assetId: "1f7b25ab-58b6-475c-a31f-05c258f99d9f",
    alt: "Close-up of the green Team Samsara jersey with a hand holding a pin",
    label: "Video",
    title: "Placeholder: a video on another website",
    href: "https://www.youtube.com",
  },
];

export const recentContent: readonly HomeTile[] = [
  {
    id: "content-1",
    assetId: "3c1caf15-e972-472e-ae31-74a7810804eb",
    alt: "Team Samsara player wearing a headset and competing at a gaming PC",
    label: "Blog",
    title: "Placeholder: a post from the blog",
    href: routePaths.about,
  },
  {
    id: "content-2",
    assetId: "1f7b25ab-58b6-475c-a31f-05c258f99d9f",
    alt: "Close-up of the green Team Samsara jersey with a hand holding a pin",
    label: "Video",
    title: "Placeholder: a highlight reel",
    href: "https://www.youtube.com",
  },
  {
    id: "content-3",
    assetId: "38075894-6cc3-40e3-81e1-8e55874e703c",
    alt: "Two Team Samsara players in green jerseys posing outdoors",
    label: "News",
    title: "Placeholder: another piece of news",
    href: routePaths.teams,
  },
];
