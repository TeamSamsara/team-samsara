// File : /team-samsara/apps/web/src/features/account/ui/AvatarPicker.tsx
// Version : 2.0.0
// Latest commit: task/web-account-settings
// Author : Gerrah
// Purpose : The profile picture chooser: the shared image picker set up for a
// round picture (JPEG, PNG or WebP, up to 2 MB) with the member's first letter
// shown until a photo exists. Nothing is uploaded here.

import type { ImageChange } from "@/shared/components/ImagePicker/imageChange";
import { ImagePicker } from "@/shared/components/ImagePicker/ImagePicker";
import {
  avatarContentTypes,
  validateAvatarFile,
} from "../validation/avatarValidation";
import styles from "./AvatarPicker.module.css";
import { AvatarPickerText } from "./avatarPickerContent";

interface AvatarPickerProps {
  controls: "inline" | "menu";
  initial: string;
  savedUrl?: string | null;
  change: ImageChange;
  error?: string | null;
  onChange: (change: ImageChange) => void;
}

const accept = avatarContentTypes.join(",");

export function AvatarPicker({
  controls,
  initial,
  savedUrl = null,
  change,
  error,
  onChange,
}: AvatarPickerProps) {
  return (
    <ImagePicker
      shape="circle"
      controls={controls}
      labels={AvatarPickerText.labels}
      savedUrl={savedUrl}
      change={change}
      onChange={onChange}
      validate={validateAvatarFile}
      accept={accept}
      placeholder={<span className={styles.initial}>{initial}</span>}
      hint={controls === "inline" ? AvatarPickerText.hint : undefined}
      error={error}
    />
  );
}
