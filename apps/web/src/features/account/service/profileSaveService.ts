// File : /team-samsara/apps/web/src/features/account/service/profileSaveService.ts
// Version : 1.0.0
// Latest commit: task/web-account-settings
// Author : Gerrah
// Purpose : Saves the edit-profile form as a sequence of API calls: name and
// bio, handle, picture, then banner. Only what changed is sent, and every call
// can be repeated safely, so after a failure the member simply tries again.

import type { ApiOutcome } from "@/features/auth/service/accountOutcome";
import type { ImageChange } from "@/shared/components/ImagePicker/imageChange";
import type {
  ImageKind,
  ProfileResult,
  ProfileStatus,
  ProfileView,
} from "../types/profileTypes";
import {
  clearImage,
  setHandle,
  setImage,
  updateProfile,
} from "./profileService";

export type ProfileSaveField =
  "displayName" | "bio" | "handle" | "picture" | "banner";

export type ProfileSaveAttempt =
  | { readonly kind: "saved"; readonly profile: ProfileView | null }
  | {
      readonly kind: "rejected";
      readonly field: ProfileSaveField;
      readonly status: ProfileStatus;
    }
  | { readonly kind: "rateLimited"; readonly retryAfterSeconds: number | null }
  | { readonly kind: "unavailable" }
  | { readonly kind: "unknown" };

export interface ProfileSaveInput {
  readonly current: ProfileView;
  readonly displayName: string;
  readonly bio: string;
  readonly handle: string;
  readonly picture: ImageChange;
  readonly banner: ImageChange;
}

type StepFailure = Exclude<ProfileSaveAttempt, { kind: "saved" | "rejected" }>;

type Step =
  | { readonly kind: "done"; readonly profile: ProfileView | null }
  | { readonly kind: "stop"; readonly attempt: ProfileSaveAttempt };

const skipped: Step = { kind: "done", profile: null };

function stop(attempt: StepFailure): Step {
  return { kind: "stop", attempt };
}

function toStep(
  outcome: ApiOutcome<ProfileResult>,
  field: ProfileSaveField,
): Step {
  switch (outcome.kind) {
    case "answer":
      return outcome.value.status === "success"
        ? { kind: "done", profile: outcome.value.profile }
        : {
            kind: "stop",
            attempt: {
              kind: "rejected",
              field,
              status: outcome.value.status,
            },
          };
    case "rateLimited":
      return stop({
        kind: "rateLimited",
        retryAfterSeconds: outcome.retryAfterSeconds,
      });
    case "unavailable":
      return stop({ kind: "unavailable" });
    default:
      return stop({ kind: "unknown" });
  }
}

function emptyToNull(value: string): string | null {
  const trimmed = value.trim();

  return trimmed.length > 0 ? trimmed : null;
}

async function saveImage(
  kind: ImageKind,
  change: ImageChange,
  field: ProfileSaveField,
): Promise<Step> {
  switch (change.kind) {
    case "replace":
      return toStep(await setImage(kind, change.file), field);
    case "remove":
      return toStep(await clearImage(kind), field);
    default:
      return skipped;
  }
}

// Sends what changed, in order, and stops at the first refusal or failure.
export async function saveProfileChanges(
  input: ProfileSaveInput,
): Promise<ProfileSaveAttempt> {
  const displayName = input.displayName.trim();
  const bio = emptyToNull(input.bio);
  const { current } = input;

  const detailsChanged =
    displayName !== current.displayName || bio !== current.bio;

  const steps: ReadonlyArray<() => Promise<Step>> = [
    async () =>
      detailsChanged
        ? toStep(await updateProfile({ displayName, bio }), "displayName")
        : skipped,
    async () =>
      input.handle === current.handle
        ? skipped
        : toStep(await setHandle({ handle: input.handle }), "handle"),
    () => saveImage("picture", input.picture, "picture"),
    () => saveImage("banner", input.banner, "banner"),
  ];

  let latest: ProfileView | null = null;

  for (const run of steps) {
    const step = await run();

    if (step.kind === "stop") {
      return step.attempt;
    }

    latest = step.profile ?? latest;
  }

  return { kind: "saved", profile: latest };
}
