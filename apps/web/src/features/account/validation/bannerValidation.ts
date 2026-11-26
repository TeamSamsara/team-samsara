// File : /team-samsara/apps/web/src/features/account/validation/bannerValidation.ts
// Version : 1.0.0
// Latest commit: task/web-account-settings
// Author : Gerrah
// Purpose : Checks a chosen banner image before it is uploaded. The API checks
// the real file again, so these only save a trip.

import { avatarContentTypes } from "./avatarValidation";
import { ProfileValidationText } from "./profileValidationContent";

export const bannerMaxBytes = 5 * 1024 * 1024;
export const bannerContentTypes = avatarContentTypes;

export function validateBannerFile(file: File): string | null {
  if (!(bannerContentTypes as readonly string[]).includes(file.type)) {
    return ProfileValidationText.avatarType;
  }

  if (file.size > bannerMaxBytes) {
    return ProfileValidationText.avatarTooBig(bannerMaxBytes / (1024 * 1024));
  }

  return null;
}
