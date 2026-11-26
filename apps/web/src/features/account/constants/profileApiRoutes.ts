// File : /team-samsara/apps/web/src/features/account/constants/profileApiRoutes.ts
// Version : 1.0.0
// Latest commit: task/web-auth-onboarding-navbar
// Author : Gerrah
// Purpose : Backend API routes for the member's own profile and for the
// public profile lookup.

export const ProfileApiRoutes = {
  profile: "/account/profile",
  handleAvailable: "/account/profile/handle/available",
  handle: "/account/profile/handle",
  onboardingComplete: "/account/profile/onboarding/complete",
  picture: "/account/profile/picture",
  banner: "/account/profile/banner",

  publicProfile: "/profiles",
} as const;
