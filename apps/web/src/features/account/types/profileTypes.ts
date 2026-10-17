// File : /team-samsara/apps/web/src/features/account/types/profileTypes.ts
// Version : 1.0.0
// Latest commit: task/web-auth-onboarding-navbar
// Author : Gerrah
// Purpose : The profile API's response and request shapes. The status list
// mirrors the backend ProfileStatus enum, so an unknown value is detectable.

export const profileStatuses = [
  "success",
  "profileNotFound",
  "invalidDisplayName",
  "invalidBio",
  "imageUnsupportedType",
  "imageTooLarge",
  "invalidHandle",
  "handleTaken",
  "handleCooldown",
] as const;
export type ProfileStatus = (typeof profileStatuses)[number];

// What a member sees of their own profile
export interface ProfileView {
  readonly displayName: string;
  readonly handle: string | null;
  readonly bio: string | null;
  readonly profilePictureAssetId: string | null;
  readonly bannerAssetId: string | null;
  readonly onboardingCompleted: boolean;
}

// What anyone sees of a member (GET /profiles/{handle}); never holds an id
export interface PublicProfile {
  readonly displayName: string;
  readonly handle: string;
  readonly bio: string | null;
  readonly profilePictureAssetId: string | null;
  readonly bannerAssetId: string | null;
}

// The answer of every profile call. The profile is null for failures and for
// the availability check, which changes nothing.
export interface ProfileResult {
  readonly status: ProfileStatus;
  readonly profile: ProfileView | null;
}

export interface UpdateProfileRequest {
  readonly displayName: string;
  readonly bio: string | null;
}

export interface SetHandleRequest {
  readonly handle: string;
}

export type ImageKind = "picture" | "banner";

export type PublicProfileOutcome =
  | { readonly kind: "found"; readonly profile: PublicProfile }
  | { readonly kind: "notFound" }
  | { readonly kind: "rateLimited"; readonly retryAfterSeconds: number | null }
  | { readonly kind: "unavailable" };
