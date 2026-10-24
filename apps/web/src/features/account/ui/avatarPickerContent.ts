// File : /team-samsara/apps/web/src/features/account/ui/avatarPickerContent.ts
// Version : 1.1.0
// Latest commit: task/web-account-settings
// Author : Gerrah
// Purpose : The text of the profile picture chooser, for both its layouts: the
// buttons beside the picture (onboarding) and the camera menu (edit profile).

import type { ImagePickerLabels } from "@/shared/components/ImagePicker/ImagePicker";

export const AvatarPickerText = {
  hint: "JPEG, PNG or WebP, up to 2 MB",
  labels: {
    choose: "Choose a photo",
    change: "Change photo",
    remove: "Remove",
    menuTrigger: "Change profile photo",
    menuUpload: "Upload photo",
    menuRemove: "Remove photo",
  } satisfies ImagePickerLabels,
} as const;
