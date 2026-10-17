// File : /team-samsara/apps/web/src/features/account/view/ProfileStep.tsx
// Version : 1.1.0
// Latest commit: task/web-auth-onboarding-navbar
// Author : Gerrah
// Purpose : The "Create your profile" step: picture, display name, handle and
// bio. The handle is suggested from the name until the member edits it, and is
// checked for availability while they type. Everything is optional to skip.

import { useState, type FormEvent } from "react";
import { Button } from "@/shared/components/Button/Button";
import { TextArea } from "@/shared/components/Fields/TextArea/TextArea";
import { TextField } from "@/shared/components/Fields/TextField/TextField";
import { useHandleAvailability } from "../hooks/useHandleAvailability";
import { saveOnboarding } from "../service/onboardingService";
import type { ProfileView } from "../types/profileTypes";
import { AvatarPicker } from "../ui/AvatarPicker";
import { StepIndicator } from "../ui/StepIndicator";
import { suggestHandle } from "../validation/handleSuggestion";
import {
  normalizeHandle,
  validateBio,
  validateDisplayName,
  validateHandle,
} from "../validation/profileValidation";
import styles from "./WelcomePage.module.css";
import { useSkipOnboarding } from "./useSkipOnboarding";
import {
  OnboardingText,
  neutralDisplayName,
  onboardingFailureMessage,
  onboardingStepCount,
  ProfileStepText,
  type FieldName,
} from "./welcomePageContent";

type FieldErrors = Record<FieldName, string | null>;

const noErrors: FieldErrors = {
  displayName: null,
  bio: null,
  handle: null,
  picture: null,
  form: null,
};

interface ProfileStepProps {
  profile: ProfileView;
  onFinished: () => void;
}

export function ProfileStep({ profile, onFinished }: ProfileStepProps) {
  const [displayName, setDisplayName] = useState(
    profile.displayName === neutralDisplayName ? "" : profile.displayName,
  );
  const [bio, setBio] = useState(profile.bio ?? "");
  const [handle, setHandle] = useState(profile.handle ?? "");
  const [handleEdited, setHandleEdited] = useState(false);
  const [picture, setPicture] = useState<File | null>(null);
  const [errors, setErrors] = useState<FieldErrors>(noErrors);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const availability = useHandleAvailability(handle, profile.handle);
  const { skip, isSkipping, skipError } = useSkipOnboarding(onFinished);

  function showError(field: FieldName, message: string | null) {
    setErrors((current) => ({ ...current, [field]: message }));
  }

  // The handle follows the name until the member writes their own.
  function changeDisplayName(value: string) {
    setDisplayName(value);

    if (errors.displayName) {
      showError("displayName", validateDisplayName(value));
    }

    if (!handleEdited) {
      const suggestion = suggestHandle(value);

      if (suggestion) {
        setHandle(suggestion);
        showError("handle", null);
      }
    }
  }

  function changeHandle(value: string) {
    setHandleEdited(true);
    setHandle(normalizeHandle(value));

    if (errors.handle) {
      showError("handle", validateHandle(value));
    }
  }

  function changeBio(value: string) {
    setBio(value);

    if (errors.bio) {
      showError("bio", validateBio(value));
    }
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (isSubmitting) {
      return;
    }

    const found: FieldErrors = {
      ...noErrors,
      displayName: validateDisplayName(displayName),
      bio: validateBio(bio),
      handle:
        validateHandle(handle) ??
        (availability === "taken" ? ProfileStepText.handleTaken : null),
    };
    setErrors(found);

    if (found.displayName || found.bio || found.handle) {
      return;
    }

    setIsSubmitting(true);
    const attempt = await saveOnboarding({
      displayName,
      bio,
      handle: normalizeHandle(handle),
      currentHandle: profile.handle,
      picture,
    });

    if (attempt.kind === "saved") {
      onFinished();
      return;
    }

    setIsSubmitting(false);

    if (attempt.kind === "rejected") {
      const { field, message } = ProfileStepText.rejection(attempt.status);
      setErrors({ ...noErrors, [field]: message });
      return;
    }

    showError("form", onboardingFailureMessage(attempt));
  }

  const handleStatusText = ProfileStepText.handleStatus(availability);
  const handleError =
    errors.handle ??
    (availability === "taken" ? ProfileStepText.handleTaken : null) ??
    (availability === "invalid" ? ProfileStepText.handleInvalid : null);
  const formError = errors.form ?? skipError;

  return (
    <div className={styles.profile}>
      <StepIndicator
        current={2}
        total={onboardingStepCount}
        label={OnboardingText.stepLabel(2, onboardingStepCount)}
      />
      <h1 className={styles.visuallyHidden}>{ProfileStepText.title}</h1>

      <form
        className={styles.form}
        onSubmit={(event) => void submit(event)}
        noValidate
      >
        <AvatarPicker
          initial={(displayName.trim() || neutralDisplayName).charAt(0)}
          error={errors.picture}
          onChange={setPicture}
        />
        <TextField
          label={ProfileStepText.displayNameLabel}
          placeholder={ProfileStepText.displayNameLabel}
          name="displayName"
          autoComplete="nickname"
          value={displayName}
          error={errors.displayName}
          onChange={(event) => changeDisplayName(event.target.value)}
          onBlur={() =>
            showError("displayName", validateDisplayName(displayName))
          }
        />
        <TextField
          label={ProfileStepText.handleLabel}
          placeholder={ProfileStepText.handleLabel}
          name="handle"
          autoComplete="off"
          autoCapitalize="none"
          spellCheck={false}
          value={handle}
          error={handleError}
          trailing={
            handleStatusText ? (
              <span className={styles.handleStatus}>{handleStatusText}</span>
            ) : null
          }
          onChange={(event) => changeHandle(event.target.value)}
          onBlur={() => showError("handle", validateHandle(handle))}
        />
        <p className={styles.hint}>{ProfileStepText.handleHint}</p>
        <TextArea
          label={ProfileStepText.bioLabel}
          placeholder={ProfileStepText.bioLabel}
          name="bio"
          value={bio}
          error={errors.bio}
          onChange={(event) => changeBio(event.target.value)}
          onBlur={() => showError("bio", validateBio(bio))}
        />

        {formError ? (
          <p className={styles.error} role="alert">
            {formError}
          </p>
        ) : null}

        <Button type="submit" fullWidth isLoading={isSubmitting}>
          {ProfileStepText.submit}
        </Button>
      </form>

      <button
        type="button"
        className={styles.linkButton}
        disabled={isSkipping || isSubmitting}
        onClick={() => void skip()}
      >
        {ProfileStepText.skip}
      </button>
    </div>
  );
}
