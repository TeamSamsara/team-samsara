// File : /team-samsara/apps/web/src/features/account/validation/profileValidation.ts
// Version : 1.0.0
// Latest commit: task/web-auth-onboarding-navbar
// Author : Gerrah
// Purpose : The rules a profile form checks before calling the API. Each
// returns the message to show, or null when the value is fine. The API
// repeats every check, including the reserved and blocked words; these only
// save a trip.

import { ProfileValidationText } from "./profileValidationContent";

/** Limits; they mirror Identity:Profile in the API's settings. */
export const displayNameMinLength = 1;
export const displayNameMaxLength = 50;
export const bioMaxLength = 300;
export const handleMinLength = 3;
export const handleMaxLength = 20;

const handlePattern = /^[a-z0-9_]+$/;

// Counts what a reader sees as characters, so an emoji counts once.
function visibleLength(value: string): number {
  return [...new Intl.Segmenter().segment(value)].length;
}

export function validateDisplayName(value: string): string | null {
  const name = value.trim();

  if (visibleLength(name) < displayNameMinLength) {
    return ProfileValidationText.displayNameRequired;
  }

  if (visibleLength(name) > displayNameMaxLength) {
    return ProfileValidationText.displayNameTooLong(displayNameMaxLength);
  }

  return null;
}

export function validateBio(value: string): string | null {
  return visibleLength(value.trim()) > bioMaxLength
    ? ProfileValidationText.bioTooLong(bioMaxLength)
    : null;
}

/** The handle as typed: lowercase, without a leading @ and surrounding spaces. */
export function normalizeHandle(value: string): string {
  return value.trim().replace(/^@+/, "").toLowerCase();
}

export function validateHandle(value: string): string | null {
  const handle = normalizeHandle(value);

  if (handle.length === 0) {
    return ProfileValidationText.handleRequired;
  }

  if (handle.length < handleMinLength) {
    return ProfileValidationText.handleTooShort(handleMinLength);
  }

  if (handle.length > handleMaxLength) {
    return ProfileValidationText.handleTooLong(handleMaxLength);
  }

  if (!handlePattern.test(handle)) {
    return ProfileValidationText.handleCharacters;
  }

  if (handle.startsWith("_") || handle.endsWith("_")) {
    return ProfileValidationText.handleUnderscoreEdge;
  }

  return null;
}
