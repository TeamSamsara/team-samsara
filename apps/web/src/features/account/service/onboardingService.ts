// File : /team-samsara/apps/web/src/features/account/service/onboardingService.ts
// Version : 1.0.0
// Latest commit: task/web-auth-onboarding-navbar
// Author : Gerrah
// Purpose : Saves the first-time profile step as a sequence of API calls:
// name and bio, handle, picture, then the "onboarding done" mark. Every call
// can be repeated safely, so after a failure the member simply tries again.

import type { ApiOutcome } from "@/features/auth/service/accountOutcome";
import type { ProfileResult, ProfileStatus } from "../types/profileTypes";
import {
  completeOnboarding,
  setHandle,
  setImage,
  updateProfile,
} from "./profileService";

export type OnboardingField = "displayName" | "bio" | "handle" | "picture";

export type OnboardingAttempt =
  | { readonly kind: "saved" }
  /** The API refused one field; the screen shows the message under it. */
  | {
      readonly kind: "rejected";
      readonly field: OnboardingField;
      readonly status: ProfileStatus;
    }
  | { readonly kind: "rateLimited"; readonly retryAfterSeconds: number | null }
  | { readonly kind: "unavailable" }
  | { readonly kind: "unknown" };

export interface OnboardingInput {
  readonly displayName: string;
  readonly bio: string;
  readonly handle: string;
  /** The handle the member holds now; it is only sent when it changed. */
  readonly currentHandle: string | null;
  readonly picture: File | null;
}

type StepFailure = Exclude<OnboardingAttempt, { kind: "saved" | "rejected" }>;

// The result of one step: it went through, or why the whole save stops here.
type Step =
  | { readonly kind: "done" }
  | { readonly kind: "stop"; readonly attempt: OnboardingAttempt };

function toStep(
  outcome: ApiOutcome<ProfileResult>,
  field: OnboardingField,
): Step {
  switch (outcome.kind) {
    case "answer":
      return outcome.value.status === "success"
        ? { kind: "done" }
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

function stop(attempt: StepFailure): Step {
  return { kind: "stop", attempt };
}

// Saves what the member filled in, then marks onboarding as done.
export async function saveOnboarding(
  input: OnboardingInput,
): Promise<OnboardingAttempt> {
  const bio = input.bio.trim();

  const steps: ReadonlyArray<() => Promise<Step>> = [
    async () =>
      toStep(
        await updateProfile({
          displayName: input.displayName.trim(),
          bio: bio.length > 0 ? bio : null,
        }),
        "displayName",
      ),
    async () =>
      input.handle === input.currentHandle
        ? { kind: "done" }
        : toStep(await setHandle({ handle: input.handle }), "handle"),
    async () =>
      input.picture === null
        ? { kind: "done" }
        : toStep(await setImage("picture", input.picture), "picture"),
    async () => toStep(await completeOnboarding(), "displayName"),
  ];

  for (const run of steps) {
    const step = await run();

    if (step.kind === "stop") {
      return step.attempt;
    }
  }

  return { kind: "saved" };
}

// Marks onboarding as done without changing the profile.
export async function skipOnboarding(): Promise<OnboardingAttempt> {
  const step = toStep(await completeOnboarding(), "displayName");

  return step.kind === "done" ? { kind: "saved" } : step.attempt;
}
