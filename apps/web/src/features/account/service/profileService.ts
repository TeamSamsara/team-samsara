// File : /team-samsara/apps/web/src/features/account/service/profileService.ts
// Version : 1.1.0
// Latest commit: task/web-account-settings
// Author : Gerrah
// Purpose : Every call to the profile API, one function per step. The
// member's own calls return an ApiOutcome and never throw; the public lookup
// returns its own outcome because "no such member" is an expected answer.

import {
  isObject,
  isOneOf,
  postEmpty,
  sendAccountRequest,
  type ApiOutcome,
} from "@/features/auth/service/accountOutcome";
import { apiFetchResult } from "@/lib/apiClient";
import { ProfileApiRoutes } from "../constants/profileApiRoutes";
import {
  profileStatuses,
  type ImageKind,
  type ProfileResult,
  type ProfileStatus,
  type ProfileView,
  type PublicProfile,
  type PublicProfileOutcome,
  type SetHandleRequest,
  type UpdateProfileRequest,
} from "../types/profileTypes";

const HttpStatus = { notFound: 404, tooManyRequests: 429 } as const;

const jsonHeaders = { "Content-Type": "application/json" } as const;

// ---- Parsers ----

function isTextOrNull(value: unknown): value is string | null {
  return value === null || typeof value === "string";
}

function readProfileView(body: unknown): ProfileView | null {
  if (
    !isObject(body) ||
    typeof body.displayName !== "string" ||
    !isTextOrNull(body.handle ?? null) ||
    !isTextOrNull(body.bio ?? null) ||
    !isTextOrNull(body.profilePictureAssetId ?? null) ||
    !isTextOrNull(body.bannerAssetId ?? null) ||
    !isTextOrNull(body.handleChangeableAt ?? null) ||
    typeof body.onboardingCompleted !== "boolean"
  ) {
    return null;
  }

  return {
    displayName: body.displayName,
    handle: (body.handle ?? null) as string | null,
    bio: (body.bio ?? null) as string | null,
    profilePictureAssetId: (body.profilePictureAssetId ?? null) as
      string | null,
    bannerAssetId: (body.bannerAssetId ?? null) as string | null,
    handleChangeableAt: (body.handleChangeableAt ?? null) as string | null,
    onboardingCompleted: body.onboardingCompleted,
  };
}

function parseProfileResult(body: unknown): ProfileResult | null {
  if (!isObject(body) || !isOneOf(profileStatuses, body.status)) {
    return null;
  }

  const status: ProfileStatus = body.status;

  if (body.profile === null || body.profile === undefined) {
    return { status, profile: null };
  }

  const profile = readProfileView(body.profile);

  // A profile that cannot be read is not trusted, whatever the status says.
  return profile === null ? null : { status, profile };
}

function readPublicProfile(body: unknown): PublicProfile | null {
  if (
    !isObject(body) ||
    typeof body.displayName !== "string" ||
    typeof body.handle !== "string" ||
    !isTextOrNull(body.bio ?? null) ||
    !isTextOrNull(body.profilePictureAssetId ?? null) ||
    !isTextOrNull(body.bannerAssetId ?? null)
  ) {
    return null;
  }

  return {
    displayName: body.displayName,
    handle: body.handle,
    bio: (body.bio ?? null) as string | null,
    profilePictureAssetId: (body.profilePictureAssetId ?? null) as
      string | null,
    bannerAssetId: (body.bannerAssetId ?? null) as string | null,
  };
}

// ---- Requests ----

function sendJson(method: "PUT", payload: object): RequestInit {
  return { method, headers: jsonHeaders, body: JSON.stringify(payload) };
}

function sendFile(file: File): RequestInit {
  const form = new FormData();
  form.append("file", file);

  // No Content-Type: the browser adds it, with the form boundary.
  return { method: "PUT", body: form };
}

const sendDelete: RequestInit = { method: "DELETE" };

function imageRoute(kind: ImageKind): string {
  return kind === "picture"
    ? ProfileApiRoutes.picture
    : ProfileApiRoutes.banner;
}

// ---- My profile ----

export function getProfile(): Promise<ApiOutcome<ProfileResult>> {
  return sendAccountRequest(ProfileApiRoutes.profile, parseProfileResult);
}

export function updateProfile(
  request: UpdateProfileRequest,
): Promise<ApiOutcome<ProfileResult>> {
  return sendAccountRequest(
    ProfileApiRoutes.profile,
    parseProfileResult,
    sendJson("PUT", request),
  );
}

// ---- Handle ----

// Asks whether the handle could be taken, without changing anything.
export function checkHandle(
  handle: string,
): Promise<ApiOutcome<ProfileResult>> {
  const query = new URLSearchParams({ handle }).toString();

  return sendAccountRequest(
    `${ProfileApiRoutes.handleAvailable}?${query}`,
    parseProfileResult,
  );
}

export function setHandle(
  request: SetHandleRequest,
): Promise<ApiOutcome<ProfileResult>> {
  return sendAccountRequest(
    ProfileApiRoutes.handle,
    parseProfileResult,
    sendJson("PUT", request),
  );
}

// ---- Onboarding ----

// Marks the first-time profile step as finished or skipped.
export function completeOnboarding(): Promise<ApiOutcome<ProfileResult>> {
  return sendAccountRequest(
    ProfileApiRoutes.onboardingComplete,
    parseProfileResult,
    postEmpty,
  );
}

// ---- Images ----

export function setImage(
  kind: ImageKind,
  file: File,
): Promise<ApiOutcome<ProfileResult>> {
  return sendAccountRequest(
    imageRoute(kind),
    parseProfileResult,
    sendFile(file),
  );
}

export function clearImage(
  kind: ImageKind,
): Promise<ApiOutcome<ProfileResult>> {
  return sendAccountRequest(imageRoute(kind), parseProfileResult, sendDelete);
}

// ---- Public profile (no sign-in) ----

export async function getPublicProfile(
  handle: string,
): Promise<PublicProfileOutcome> {
  let result;

  try {
    result = await apiFetchResult<unknown>(
      `${ProfileApiRoutes.publicProfile}/${encodeURIComponent(handle)}`,
    );
  } catch {
    return { kind: "unavailable" };
  }

  if (result.ok) {
    const profile = readPublicProfile(result.body);

    return profile === null
      ? { kind: "unavailable" }
      : { kind: "found", profile };
  }

  switch (result.httpStatus) {
    case HttpStatus.notFound:
      return { kind: "notFound" };
    case HttpStatus.tooManyRequests:
      return {
        kind: "rateLimited",
        retryAfterSeconds: result.retryAfterSeconds,
      };
    default:
      return { kind: "unavailable" };
  }
}
