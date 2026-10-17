// File : /team-samsara/apps/web/src/features/account/data/onboardingSlides.ts
// Version : 1.0.0
// Latest commit: task/web-auth-onboarding-navbar
// Author : Gerrah
// Purpose : The photos that change on the left of the onboarding screens.
// They reuse the home hero photos until onboarding has its own.

import { heroSlides } from "@/features/home/data/heroSlides";

export interface OnboardingSlide {
  readonly id: string;
  readonly assetId: string;
  readonly alt: string;
  readonly focus?: string;
}

export const onboardingSlides: readonly OnboardingSlide[] = heroSlides.map(
  ({ id, assetId, alt, focus }) => ({ id, assetId, alt, focus }),
);

export const onboardingSlideAutoplayMs = 6000;
