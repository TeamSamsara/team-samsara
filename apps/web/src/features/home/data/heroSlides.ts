// File : /team-samsara/apps/web/src/features/home/data/heroSlides.ts
// Version : 1.1.1
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : Content of the home hero carousel. Each slide points at an image
// stored through the backend Assets module (by asset id, resolved to a URL with
// assetUrl) plus the text and call to action shown over it. This file stands in
// for the CMS until it is built: the CMS query will later return the same
// HeroSlide shape, so the hero component will not need to change.

import { routePaths } from "../../../shared/constants/routePaths";

export type HeroSlide = {
  /** Stable key for React lists. */
  id: string;
  /** Asset id returned by POST /assets (Staging). */
  assetId: string;
  /** Describes the photo for screen readers. */
  alt: string;
  /**
   * Which part of the photo stays in view when it is cropped to the hero, as
   * a CSS object-position ("50% 10%" keeps the horizontal center and sits
   * near the top). Leave out to center the photo.
   */
  focus?: string;
  title: string;
  subtitle: string;
  cta: {
    label: string;
    to: string;
  };
};

const brandTitle = "TEAM SAMSARA";
const brandSubtitle = "EVOLVE - WIN - REPEAT";
const brandCta = { label: "JOIN US", to: routePaths.register };

export const heroSlides: readonly HeroSlide[] = [
  {
    id: "aeon-1",
    assetId: "3c1caf15-e972-472e-ae31-74a7810804eb",
    alt: "Team Samsara player wearing a headset and competing at a gaming PC",
    title: brandTitle,
    subtitle: brandSubtitle,
    cta: brandCta,
  },
  {
    id: "aeon-2",
    assetId: "72673d9a-81c3-4316-a749-b03aeb10bbac",
    alt: "Smiling Team Samsara player in a green jersey making a peace sign",
    focus: "50% 10%",
    title: brandTitle,
    subtitle: brandSubtitle,
    cta: brandCta,
  },
  {
    id: "champions",
    assetId: "1f7b25ab-58b6-475c-a31f-05c258f99d9f",
    alt: "Close-up of the green Team Samsara jersey with a hand holding a pin",
    title: brandTitle,
    subtitle: brandSubtitle,
    cta: brandCta,
  },
  {
    id: "kappa-1",
    assetId: "1745dbc1-000a-4804-a8b2-05e258f7c5a7",
    alt: "Team Samsara player in a green jersey seated at a gaming setup",
    title: brandTitle,
    subtitle: brandSubtitle,
    cta: brandCta,
  },
  {
    id: "kappa-beep-1",
    assetId: "38075894-6cc3-40e3-81e1-8e55874e703c",
    alt: "Two Team Samsara players in green jerseys posing outdoors",
    title: brandTitle,
    subtitle: brandSubtitle,
    cta: brandCta,
  },
];
