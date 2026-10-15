// File : /team-samsara/apps/web/src/lib/devInvariants.ts
// Version : 1.1.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : Developer-facing invariant/assertion messages - text only a developer debugging
// a broken setup would ever see, never a real user. Kept separate from uiMessages.ts since
// the two categories have different owners and different audiences, even though both are
// semantic strings under the same magic-value convention.

export const DevInvariants = {
  useAuthOutsideProvider: "useAuth must be used within an AuthProvider",
  carouselPartOutsideCarousel:
    "Carousel parts must be used inside a <Carousel> component",
} as const;
