// File : /team-samsara/apps/web/src/features/home/ui/homeHeroContent.ts
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The fixed text and timing of the home hero. The slide content
// (photo, title, subtitle, call to action) lives in data/heroSlides.ts.

export const HomeHeroText = {
  carouselLabel: "Team Samsara highlights",
  goToSlide: (slideNumber: number) => `Go to slide ${slideNumber}`,
} as const;

/** Time each slide stays on screen before the next one fades in. */
export const homeHeroAutoplayMs = 6000;
