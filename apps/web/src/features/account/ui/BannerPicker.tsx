// File : /team-samsara/apps/web/src/features/account/ui/BannerPicker.tsx
// Version : 1.0.0
// Latest commit: task/web-account-settings
// Author : Gerrah
// Purpose : The profile banner chooser: the shared image picker set up for a
// wide picture (JPEG, PNG or WebP, up to 5 MB) with a camera button on it.
// Nothing is uploaded here.

import type { ImageChange } from "@/shared/components/ImagePicker/imageChange";
import { ImagePicker } from "@/shared/components/ImagePicker/ImagePicker";
import {
  bannerContentTypes,
  validateBannerFile,
} from "../validation/bannerValidation";
import { BannerPickerText } from "./bannerPickerContent";

interface BannerPickerProps {
  savedUrl: string | null;
  change: ImageChange;
  error?: string | null;
  onChange: (change: ImageChange) => void;
}

const accept = bannerContentTypes.join(",");

export function BannerPicker({
  savedUrl,
  change,
  error,
  onChange,
}: BannerPickerProps) {
  return (
    <ImagePicker
      shape="banner"
      controls="menu"
      labels={BannerPickerText.labels}
      savedUrl={savedUrl}
      change={change}
      onChange={onChange}
      validate={validateBannerFile}
      accept={accept}
      error={error}
    />
  );
}
