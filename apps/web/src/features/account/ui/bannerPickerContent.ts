// File : /team-samsara/apps/web/src/features/account/ui/bannerPickerContent.ts
// Version : 1.0.0
// Latest commit: task/web-account-settings
// Author : Gerrah
// Purpose : The text of the profile banner chooser.

import type { ImagePickerLabels } from "@/shared/components/ImagePicker/ImagePicker";

export const BannerPickerText = {
  hint: "JPEG, PNG or WebP, up to 5 MB",
  labels: {
    choose: "Choose a banner",
    change: "Change banner",
    remove: "Remove",
    menuTrigger: "Change banner",
    menuUpload: "Upload banner",
    menuRemove: "Remove banner",
  } satisfies ImagePickerLabels,
} as const;
