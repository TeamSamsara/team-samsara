// File : /team-samsara/apps/web/src/features/account/view/ProfileSettingsForm.tsx
// Version : 1.4.0
// Latest commit: task/web-member-profile
// Author : Gerrah
// Purpose : The profile editor, shown in place of the profile page: the same
// layout with a banner, an overlapping picture, the name, handle and bio as
// fields, and Cancel and Save changes. The state and the saving live in
// useProfileForm. Cancel and Discard hand control back through onLeave, and a
// successful save through onSaved. Leaving with unsaved changes asks first,
// whether by Cancel, a link, the back button, or closing the tab.

import { useEffect, useId, useRef, useState, type FormEvent } from "react";
import { useBlocker } from "react-router-dom";
import { assetUrl } from "@/lib/assetUrl";
import { InlineMessage } from "@/shared/components/InlineMessage/InlineMessage";
import { Modal } from "@/shared/components/Modal/Modal";
import { useLanguage } from "@/shared/hooks/useLanguage";
import { Icon } from "@/shared/icons/Icon";
import type { IconName } from "@/shared/icons/iconRegistry";
import { useProfileForm } from "../hooks/useProfileForm";
import type { ProfileView } from "../types/profileTypes";
import { AvatarPicker } from "../ui/AvatarPicker";
import { BannerPicker } from "../ui/BannerPicker";
import {
  bioMaxLength,
  displayNameMaxLength,
  handleMaxLength,
  normalizeHandle,
  visibleLength,
} from "../validation/profileValidation";
import styles from "./ProfileSettingsPage.module.css";
import { ProfileSettingsText } from "./profileSettingsContent";

type NoteTone = "muted" | "ok" | "error";

interface Note {
  text: string;
  tone: NoteTone;
  icon: IconName;
}

interface ProfileSettingsFormProps {
  profile: ProfileView;
  /** Called when the member is done with the editor without saving. */
  onLeave: () => void;
  /** Called after a save, with the handle the profile now has. */
  onSaved: (handle: string) => void;
}

const noteClass: Record<NoteTone, string> = {
  muted: styles.noteMuted,
  ok: styles.noteOk,
  error: styles.noteError,
};

export function ProfileSettingsForm({
  profile,
  onLeave,
  onSaved,
}: ProfileSettingsFormProps) {
  const { language } = useLanguage();
  const form = useProfileForm(profile);
  const [isConfirmingCancel, setIsConfirmingCancel] = useState(false);
  // Set once a save went through, so the move to a new handle is not mistaken for leaving.
  const hasSavedRef = useRef(false);

  const blocker = useBlocker(
    ({ currentLocation, nextLocation }) =>
      form.isDirty &&
      !hasSavedRef.current &&
      currentLocation.pathname !== nextLocation.pathname,
  );
  const isConfirmingLeave = isConfirmingCancel || blocker.state === "blocked";

  const nameId = useId();
  const handleId = useId();
  const bioId = useId();

  // The browser's own prompt when the tab is closed or refreshed with unsaved changes.
  useEffect(() => {
    if (!form.isDirty) {
      return;
    }

    function warn(event: BeforeUnloadEvent) {
      event.preventDefault();
    }

    window.addEventListener("beforeunload", warn);
    return () => window.removeEventListener("beforeunload", warn);
  }, [form.isDirty]);

  function cancel() {
    if (form.isDirty) {
      setIsConfirmingCancel(true);
      return;
    }

    onLeave();
  }

  function keepEditing() {
    setIsConfirmingCancel(false);

    if (blocker.state === "blocked") {
      blocker.reset();
    }
  }

  function discard() {
    setIsConfirmingCancel(false);

    // A blocked link or back button goes ahead; Cancel puts the saved values back and leaves.
    if (blocker.state === "blocked") {
      blocker.proceed();
      return;
    }

    form.discard();
    onLeave();
  }

  async function saveAndFinish() {
    const saved = await form.submit();

    if (saved) {
      hasSavedRef.current = true;
      onSaved(
        form.handleLocked
          ? (profile.handle ?? "")
          : normalizeHandle(form.handle),
      );
    }
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    void saveAndFinish();
  }

  function handleNote(): Note {
    if (form.handleLocked && form.handleChangeableAt) {
      const date = new Intl.DateTimeFormat(language, {
        dateStyle: "long",
      }).format(new Date(form.handleChangeableAt));

      return {
        text: ProfileSettingsText.handleLockedUntil(date),
        tone: "muted",
        icon: "calendar",
      };
    }

    if (form.errors.handle) {
      return { text: form.errors.handle, tone: "error", icon: "warning" };
    }

    const text = ProfileSettingsText.handleNote(form.availability);

    if (form.availability === "available") {
      return { text, tone: "ok", icon: "check" };
    }

    if (form.availability === "taken" || form.availability === "invalid") {
      return { text, tone: "error", icon: "warning" };
    }

    return {
      text: text || ProfileSettingsText.handleHint,
      tone: "muted",
      icon: "info",
    };
  }

  const note = handleNote();
  const savedPicture = profile.profilePictureAssetId
    ? assetUrl(profile.profilePictureAssetId)
    : null;
  const savedBanner = profile.bannerAssetId
    ? assetUrl(profile.bannerAssetId)
    : null;
  const initial = (form.displayName.trim() || profile.displayName).charAt(0);

  return (
    <form className={styles.page} onSubmit={submit} noValidate>
      <h1 className={styles.visuallyHidden}>{ProfileSettingsText.title}</h1>

      <div className={styles.banner}>
        <BannerPicker
          savedUrl={savedBanner}
          change={form.banner}
          error={form.errors.banner}
          onChange={form.changeBanner}
        />
      </div>

      <div className={styles.identity}>
        <div className={styles.avatar}>
          <AvatarPicker
            controls="menu"
            initial={initial}
            savedUrl={savedPicture}
            change={form.picture}
            error={form.errors.picture}
            onChange={form.changePicture}
          />
        </div>

        <div className={styles.actions}>
          <button
            type="button"
            className={styles.outline}
            onClick={cancel}
            disabled={form.isSaving}
          >
            {ProfileSettingsText.cancel}
          </button>
          <button
            type="submit"
            className={styles.save}
            disabled={!form.isDirty || form.isSaving}
            aria-busy={form.isSaving || undefined}
          >
            <span>{ProfileSettingsText.save}</span>
          </button>
        </div>

        <div className={styles.fields}>
          <div className={styles.field}>
            <label className={styles.label} htmlFor={nameId}>
              {ProfileSettingsText.displayNameLabel}
            </label>
            <input
              id={nameId}
              className={`${styles.box} ${styles.name} ${
                form.errors.displayName ? styles.invalid : ""
              }`}
              name="displayName"
              autoComplete="nickname"
              maxLength={displayNameMaxLength * 2}
              value={form.displayName}
              aria-invalid={Boolean(form.errors.displayName) || undefined}
              aria-describedby={
                form.errors.displayName ? `${nameId}-note` : undefined
              }
              onChange={(event) => form.changeDisplayName(event.target.value)}
              onBlur={form.checkDisplayName}
            />
            {form.errors.displayName ? (
              <p
                id={`${nameId}-note`}
                className={`${styles.note} ${styles.noteError}`}
                role="alert"
              >
                <Icon name="warning" className={styles.noteIcon} />
                {form.errors.displayName}
              </p>
            ) : null}
          </div>

          <div className={styles.field}>
            <label className={styles.label} htmlFor={handleId}>
              {ProfileSettingsText.handleLabel}
            </label>
            <div
              className={`${styles.box} ${styles.handleBox} ${
                form.handleLocked ? styles.locked : ""
              } ${form.errors.handle ? styles.invalid : ""}`}
            >
              <span className={styles.prefix} aria-hidden="true">
                {ProfileSettingsText.handlePrefix}
              </span>
              <input
                id={handleId}
                className={styles.bare}
                name="handle"
                autoComplete="off"
                autoCapitalize="none"
                spellCheck={false}
                maxLength={handleMaxLength * 2}
                value={form.handle}
                disabled={form.handleLocked}
                aria-invalid={Boolean(form.errors.handle) || undefined}
                aria-describedby={`${handleId}-note`}
                onChange={(event) => form.changeHandle(event.target.value)}
                onBlur={form.checkHandle}
              />
            </div>
            <p
              id={`${handleId}-note`}
              className={`${styles.note} ${noteClass[note.tone]}`}
              role={note.tone === "error" ? "alert" : undefined}
            >
              <Icon name={note.icon} className={styles.noteIcon} />
              {note.text}
            </p>
          </div>

          <div className={styles.field}>
            <label className={styles.label} htmlFor={bioId}>
              {ProfileSettingsText.bioLabel}
            </label>
            <textarea
              id={bioId}
              className={`${styles.box} ${styles.area} ${
                form.errors.bio ? styles.invalid : ""
              }`}
              name="bio"
              value={form.bio}
              aria-invalid={Boolean(form.errors.bio) || undefined}
              aria-describedby={form.errors.bio ? `${bioId}-note` : undefined}
              onChange={(event) => form.changeBio(event.target.value)}
              onBlur={form.checkBio}
            />
            <div className={styles.bioFooter}>
              {form.errors.bio ? (
                <p
                  id={`${bioId}-note`}
                  className={`${styles.note} ${styles.noteError}`}
                  role="alert"
                >
                  <Icon name="warning" className={styles.noteIcon} />
                  {form.errors.bio}
                </p>
              ) : (
                <span />
              )}
              <span className={styles.count}>
                {ProfileSettingsText.bioCount(
                  visibleLength(form.bio.trim()),
                  bioMaxLength,
                )}
              </span>
            </div>
          </div>

          <InlineMessage message={form.errors.form} withIcon />
        </div>
      </div>

      {isConfirmingLeave ? (
        <Modal title={ProfileSettingsText.discardTitle} onClose={keepEditing}>
          <p className={styles.modalText}>{ProfileSettingsText.discardBody}</p>
          <div className={styles.modalActions}>
            <button
              type="button"
              className={styles.outline}
              onClick={keepEditing}
            >
              {ProfileSettingsText.keepEditing}
            </button>
            <button type="button" className={styles.save} onClick={discard}>
              <span>{ProfileSettingsText.discard}</span>
            </button>
          </div>
        </Modal>
      ) : null}
    </form>
  );
}
