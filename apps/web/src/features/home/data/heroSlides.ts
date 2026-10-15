// File : /team-samsara/apps/web/src/features/home/data/heroSlides.ts
// Version : 1.1.0
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
    assetId: "c44b9e63-195c-49ae-a7f4-6d0112566e05",
    alt: "Team Samsara player wearing a headset and competing at a gaming PC",
    title: brandTitle,
    subtitle: brandSubtitle,
    cta: brandCta,
  },
  {
    id: "aeon-2",
    assetId: "2e381428-a63b-4f45-b6a0-ec1a190a6ad8",
    alt: "Smiling Team Samsara player in a green jersey making a peace sign",
    focus: "50% 10%",
    title: brandTitle,
    subtitle: brandSubtitle,
    cta: brandCta,
  },
  {
    id: "champions",
    assetId: "73d4e4e1-6ca1-462d-be09-ddb3b518625d",
    alt: "Close-up of the green Team Samsara jersey with a hand holding a pin",
    title: brandTitle,
    subtitle: brandSubtitle,
    cta: brandCta,
  },
  {
    id: "kappa-1",
    assetId: "a2f0d9aa-5adc-473f-8833-5f9292755868",
    alt: "Team Samsara player in a green jersey seated at a gaming setup",
    title: brandTitle,
    subtitle: brandSubtitle,
    cta: brandCta,
  },
  {
    id: "kappa-beep-1",
    assetId: "579048d6-1de3-4e53-8393-d4b1241cb78f",
    alt: "Two Team Samsara players in green jerseys posing outdoors",
    title: brandTitle,
    subtitle: brandSubtitle,
    cta: brandCta,
  },
];
