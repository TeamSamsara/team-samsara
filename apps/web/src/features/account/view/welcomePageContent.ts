// File : /team-samsara/apps/web/src/features/account/view/welcomePageContent.ts
// Version : 1.1.0
// Latest commit: task/web-auth-onboarding-navbar
// Author : Gerrah
// Purpose : The text of the two onboarding steps, including what to say when a
// save is refused.

import type { OnboardingAttempt } from "../service/onboardingService";
import type { ProfileStatus } from "../types/profileTypes";
import type { HandleAvailability } from "../hooks/useHandleAvailability";
import { AuthMessages } from "@/features/auth/ui/authMessagesContent";

/** The name every new profile starts with; it is not offered back as an answer. */
export const neutralDisplayName = "Member";

export type FieldName = "displayName" | "bio" | "handle" | "picture" | "form";

/** The welcome step and the profile step. */
export const onboardingStepCount = 2;

export const OnboardingText = {
  stepLabel: (current: number, total: number): string =>
    `Step ${current} of ${total}`,
} as const;

export const WelcomeText = {
  headlineLines: ["WELCOME TO", "SAMSARA"],
  subtitle: "Your account is ready. Let's set up your profile.",
  title: "MAKE IT YOURS",
  body: "Add a name, a handle and a picture so other members recognise you. It takes about a minute, and you can do it later from your account.",
  start: "SET UP MY PROFILE",
  skip: "Skip for now",
  loadFailed: "We couldn't load your profile. Please try again.",
  retry: "TRY AGAIN",
} as const;

export const ProfileStepText = {
  headlineLines: ["CREATE YOUR", "PROFILE"],
  subtitle: "Tell the community who you are.",
  title: "CREATE YOUR PROFILE",
  displayNameLabel: "Display name",
  bioLabel: "Bio (optional)",
  handleLabel: "Handle",
  handleHint:
    "3 to 20 letters, numbers or underscores. You can change it every 30 days.",
  submit: "SAVE PROFILE",
  skip: "Skip for now",
  handleChecking: "Checking…",
  handleAvailable: "Available",
  handleTaken: "That handle is already taken.",
  handleInvalid: "That handle can't be used.",

  handleStatus: (availability: HandleAvailability): string => {
    switch (availability) {
      case "checking":
        return ProfileStepText.handleChecking;
      case "available":
        return ProfileStepText.handleAvailable;
      default:
        return "";
    }
  },

  // Where a refused save is shown, and what it says
  rejection: (status: ProfileStatus): { field: FieldName; message: string } => {
    switch (status) {
      case "invalidDisplayName":
        return {
          field: "displayName",
          message: "That name can't be used. Try another.",
        };
      case "invalidBio":
        return {
          field: "bio",
          message: "Your bio can't be used. Try changing it.",
        };
      case "invalidHandle":
        return { field: "handle", message: ProfileStepText.handleInvalid };
      case "handleTaken":
        return { field: "handle", message: ProfileStepText.handleTaken };
      case "imageUnsupportedType":
        return { field: "picture", message: "Use a JPEG, PNG or WebP image." };
      case "imageTooLarge":
        return { field: "picture", message: "That image is too large." };
      default:
        return { field: "form", message: AuthMessages.unknown };
    }
  },
} as const;

// The message for a save that failed for a reason that is not about one field.
export function onboardingFailureMessage(attempt: OnboardingAttempt): string {
  switch (attempt.kind) {
    case "rateLimited":
      return AuthMessages.rateLimited(attempt.retryAfterSeconds);
    case "unavailable":
      return AuthMessages.unavailable;
    default:
      return AuthMessages.unknown;
  }
}
