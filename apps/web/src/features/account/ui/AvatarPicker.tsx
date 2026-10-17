// File : /team-samsara/apps/web/src/features/account/ui/AvatarPicker.tsx
// Version : 1.0.0
// Latest commit: task/web-auth-onboarding-navbar
// Author : Gerrah
// Purpose : Lets a member choose a profile picture. Shows a round preview (or the first
// letter of their name until they choose), and tells the screen which file was picked.
// Nothing is uploaded here.

import { useEffect, useRef, useState, type ChangeEvent } from "react";
import {
  avatarContentTypes,
  validateAvatarFile,
} from "../validation/avatarValidation";
import styles from "./AvatarPicker.module.css";
import { AvatarPickerText } from "./avatarPickerContent";

interface AvatarPickerProps {
  /** Shown in the circle until a photo is chosen. */
  initial: string;
  /** A message from the screen, for example the API refusing the file. */
  error?: string | null;
  onChange: (file: File | null) => void;
}

export function AvatarPicker({ initial, error, onChange }: AvatarPickerProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [fileError, setFileError] = useState<string | null>(null);

  // Keeps the latest preview address so it can be released when the picker goes away.
  const latestUrl = useRef<string | null>(null);

  useEffect(() => {
    return () => {
      if (latestUrl.current) {
        URL.revokeObjectURL(latestUrl.current);
      }
    };
  }, []);

  function showPreview(url: string | null) {
    if (latestUrl.current) {
      URL.revokeObjectURL(latestUrl.current);
    }

    latestUrl.current = url;
    setPreviewUrl(url);
  }

  function choose(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = "";

    if (!file) {
      return;
    }

    const problem = validateAvatarFile(file);
    setFileError(problem);

    if (problem) {
      return;
    }

    showPreview(URL.createObjectURL(file));
    onChange(file);
  }

  function remove() {
    setFileError(null);
    showPreview(null);
    onChange(null);
  }

  const shownError = fileError ?? error ?? null;

  return (
    <div className={styles.picker}>
      <div className={styles.circle} aria-hidden="true">
        {previewUrl ? (
          <img className={styles.image} src={previewUrl} alt="" />
        ) : (
          <span className={styles.initial}>{initial}</span>
        )}
      </div>

      <div className={styles.controls}>
        <div className={styles.buttons}>
          <button
            type="button"
            className={styles.choose}
            onClick={() => inputRef.current?.click()}
          >
            {previewUrl ? AvatarPickerText.change : AvatarPickerText.choose}
          </button>
          {previewUrl ? (
            <button type="button" className={styles.remove} onClick={remove}>
              {AvatarPickerText.remove}
            </button>
          ) : null}
        </div>

        {shownError ? (
          <p className={styles.error} role="alert">
            {shownError}
          </p>
        ) : (
          <p className={styles.hint}>{AvatarPickerText.hint}</p>
        )}
      </div>

      <input
        ref={inputRef}
        className={styles.input}
        type="file"
        accept={avatarContentTypes.join(",")}
        onChange={choose}
        tabIndex={-1}
      />
    </div>
  );
}
