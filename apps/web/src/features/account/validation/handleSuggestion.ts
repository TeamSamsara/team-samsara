// File : /team-samsara/apps/web/src/features/account/validation/handleSuggestion.ts
// Version : 1.0.0
// Latest commit: task/web-auth-onboarding-navbar
// Author : Gerrah
// Purpose : Suggests a handle from a display name, the way the API does for a
// new member: lowercase letters and digits only, accents dropped, cut to the
// handle length. Returns an empty string when the name gives too little to use.

import { handleMaxLength, handleMinLength } from "./profileValidation";

export function suggestHandle(displayName: string): string {
  const letters = displayName
    .toLowerCase()
    .normalize("NFD")
    .replace(/[^a-z0-9]/g, "");

  const handle = letters.slice(0, handleMaxLength);

  return handle.length >= handleMinLength ? handle : "";
}
