// File : /team-samsara/apps/web/src/features/account/view/useProfileForm.ts
// Version : 1.0.0
// Latest commit: task/web-account-settings
// Author : Gerrah
// Purpose : Holds the edit-profile form: what the member has typed or picked,
// what is wrong with it, whether it differs from the saved profile, and the
// save itself. The page only lays these out.

import { useState } from "react";
import {
  keepImage,
  type ImageChange,
} from "@/shared/components/ImagePicker/imageChange";
import { useHandleAvailability } from "../hooks/useHandleAvailability";
import { useSaveProfile } from "../hooks/useSaveProfile";
import type { ProfileView } from "../types/profileTypes";
import {
  normalizeHandle,
  validateBio,
  validateDisplayName,
  validateHandle,
} from "../validation/profileValidation";
import {
  ProfileSettingsText,
  profileSaveFailureMessage,
  type SettingsFieldName,
} from "./profileSettingsContent";

type FieldErrors = Record<SettingsFieldName, string | null>;

const noErrors: FieldErrors = {
  displayName: null,
  bio: null,
  handle: null,
  picture: null,
  banner: null,
  form: null,
};

export function useProfileForm(profile: ProfileView) {
  const [displayName, setDisplayName] = useState(profile.displayName);
  const [bio, setBio] = useState(profile.bio ?? "");
  const [handle, setHandle] = useState(profile.handle ?? "");
  const [picture, setPicture] = useState<ImageChange>(keepImage);
  const [banner, setBanner] = useState<ImageChange>(keepImage);
  const [errors, setErrors] = useState<FieldErrors>(noErrors);
  const [isSaved, setIsSaved] = useState(false);

  const { save, isSaving } = useSaveProfile();
  const availability = useHandleAvailability(handle, profile.handle);
  const handleLocked = profile.handleChangeableAt !== null;

  const trimmedBio = bio.trim();
  const isDirty =
    displayName.trim() !== profile.displayName ||
    (trimmedBio === "" ? null : trimmedBio) !== profile.bio ||
    normalizeHandle(handle) !== (profile.handle ?? "") ||
    picture.kind !== "keep" ||
    banner.kind !== "keep";

  function showError(field: SettingsFieldName, message: string | null) {
    setErrors((current) => ({ ...current, [field]: message }));
  }

  function changeDisplayName(value: string) {
    setDisplayName(value);
    setIsSaved(false);

    if (errors.displayName) {
      showError("displayName", validateDisplayName(value));
    }
  }

  function changeBio(value: string) {
    setBio(value);
    setIsSaved(false);

    if (errors.bio) {
      showError("bio", validateBio(value));
    }
  }

  function changeHandle(value: string) {
    setHandle(normalizeHandle(value));
    setIsSaved(false);

    if (errors.handle) {
      showError("handle", validateHandle(value));
    }
  }

  function changePicture(change: ImageChange) {
    setPicture(change);
    setIsSaved(false);
    showError("picture", null);
  }

  function changeBanner(change: ImageChange) {
    setBanner(change);
    setIsSaved(false);
    showError("banner", null);
  }

  function checkDisplayName() {
    showError("displayName", validateDisplayName(displayName));
  }

  function checkBio() {
    showError("bio", validateBio(bio));
  }

  function checkHandle() {
    if (!handleLocked) {
      showError("handle", validateHandle(handle));
    }
  }

  async function submit() {
    if (isSaving || !isDirty) {
      return;
    }

    const found: FieldErrors = {
      ...noErrors,
      displayName: validateDisplayName(displayName),
      bio: validateBio(bio),
      handle: handleLocked
        ? null
        : (validateHandle(handle) ??
          (availability === "taken" ? ProfileSettingsText.handleTaken : null)),
    };
    setErrors(found);

    if (found.displayName || found.bio || found.handle) {
      return;
    }

    setIsSaved(false);
    const attempt = await save({
      current: profile,
      displayName,
      bio,
      handle: handleLocked ? (profile.handle ?? "") : normalizeHandle(handle),
      picture,
      banner,
    });

    if (attempt.kind === "saved") {
      setDisplayName(displayName.trim());
      setBio(trimmedBio);
      setPicture(keepImage);
      setBanner(keepImage);
      setIsSaved(true);
      return;
    }

    if (attempt.kind === "rejected") {
      showError(
        attempt.field,
        ProfileSettingsText.rejectionMessage(attempt.status),
      );
      return;
    }

    showError("form", profileSaveFailureMessage(attempt));
  }

  return {
    displayName,
    bio,
    handle,
    picture,
    banner,
    errors,
    availability,
    handleLocked,
    handleChangeableAt: profile.handleChangeableAt,
    isDirty,
    isSaving,
    isSaved,
    changeDisplayName,
    changeBio,
    changeHandle,
    changePicture,
    changeBanner,
    checkDisplayName,
    checkBio,
    checkHandle,
    submit,
  };
}
