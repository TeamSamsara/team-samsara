// File : /team-samsara/apps/web/src/features/account/ui/onboardingSlideshowContent.ts
// Version : 1.0.0
// Latest commit: task/web-auth-onboarding-navbar
// Author : Gerrah
// Purpose : The text read by screen readers for the onboarding slideshow.

export const OnboardingSlideshowText = {
  carouselLabel: "Team Samsara photos",
  goToSlide: (position: number): string => `Go to photo ${position}`,
} as const;
