// File : /team-samsara/apps/web/src/features/account/view/profileSettingsContent.ts
// Version : 1.0.0
// Latest commit: task/web-account-settings
// Author : Gerrah
// Purpose : The text of the edit-profile page: field labels, the handle notes,
// the unsaved-changes question, and what to say when a save is refused or fails.

import { AuthMessages } from "@/features/auth/ui/authMessagesContent";
import type { HandleAvailability } from "../hooks/useHandleAvailability";
import type { ProfileSaveAttempt } from "../service/profileSaveService";
import type { ProfileStatus } from "../types/profileTypes";

export type SettingsFieldName =
  "displayName" | "bio" | "handle" | "picture" | "banner" | "form";

export const ProfileSettingsText = {
  title: "EDIT PROFILE",
  displayNameLabel: "Display name",
  handleLabel: "Handle",
  bioLabel: "Bio",
  handlePrefix: "@",
  handleHint:
    "3 to 20 letters, numbers or underscores. You can change it every 30 days.",
  handleChecking: "Checking…",
  handleAvailable: "That handle is available",
  handleTaken: "That handle is already taken.",
  handleInvalid: "That handle can't be used.",
  handleLockedUntil: (date: string) =>
    `You changed your handle recently. You can change it again on ${date}.`,
  bioCount: (used: number, max: number) => `${used} / ${max}`,
  bannerHint: "JPEG, PNG or WebP, up to 5 MB",
  cancel: "Cancel",
  save: "Save changes",
  saved: "Your profile was saved.",
  loadFailed: "We couldn't load your profile. Please try again.",
  retry: "TRY AGAIN",
  discardTitle: "Discard your changes?",
  discardBody: "Your changes haven't been saved yet.",
  keepEditing: "Keep editing",
  discard: "Discard changes",

  handleNote: (availability: HandleAvailability): string => {
    switch (availability) {
      case "checking":
        return ProfileSettingsText.handleChecking;
      case "available":
        return ProfileSettingsText.handleAvailable;
      case "taken":
        return ProfileSettingsText.handleTaken;
      case "invalid":
        return ProfileSettingsText.handleInvalid;
      default:
        return "";
    }
  },

  rejectionMessage: (status: ProfileStatus): string => {
    switch (status) {
      case "invalidDisplayName":
        return "That name can't be used. Try another.";
      case "invalidBio":
        return "Your bio can't be used. Try changing it.";
      case "invalidHandle":
        return ProfileSettingsText.handleInvalid;
      case "handleTaken":
        return ProfileSettingsText.handleTaken;
      case "handleCooldown":
        return "You changed your handle recently. Try again later.";
      case "imageUnsupportedType":
        return "Use a JPEG, PNG or WebP image.";
      case "imageTooLarge":
        return "That image is too large.";
      default:
        return AuthMessages.unknown;
    }
  },
} as const;

// The message for a save that failed for a reason that is not about one field.
export function profileSaveFailureMessage(
  attempt: Exclude<ProfileSaveAttempt, { kind: "saved" | "rejected" }>,
): string {
  switch (attempt.kind) {
    case "rateLimited":
      return AuthMessages.rateLimited(attempt.retryAfterSeconds);
    case "unavailable":
      return AuthMessages.unavailable;
    default:
      return AuthMessages.unknown;
  }
}
