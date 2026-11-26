// File : /team-samsara/apps/web/src/features/account/validation/avatarValidation.ts
// Version : 1.0.0
// Latest commit: task/web-auth-onboarding-navbar
// Author : Gerrah
// Purpose : Checks a chosen profile picture before it is uploaded. The API
// checks the real file again, so these only save a trip.

import { ProfileValidationText } from "./profileValidationContent";

/** Mirror the API's member image settings. */
export const avatarMaxBytes = 2 * 1024 * 1024;
export const avatarContentTypes = [
  "image/jpeg",
  "image/png",
  "image/webp",
] as const;

export function validateAvatarFile(file: File): string | null {
  if (!(avatarContentTypes as readonly string[]).includes(file.type)) {
    return ProfileValidationText.avatarType;
  }

  if (file.size > avatarMaxBytes) {
    return ProfileValidationText.avatarTooBig(avatarMaxBytes / (1024 * 1024));
  }

  return null;
}
