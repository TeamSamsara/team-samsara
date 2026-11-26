// File : /team-samsara/apps/web/src/lib/apiClient.ts
// Version : 1.2.0
// Latest commit: task/web-auth-foundation
// Author : Gerrah
// Purpose : Thin fetch wrapper used by every feature's service layer.
// apiFetch throws ApiError on any failure. apiFetchResult returns the status, JSON body
// and Retry-After seconds instead, for endpoints whose non-2xx answers are expected outcomes.

import { getAuth } from "firebase/auth";
import { appConfig } from "@/config/appConfig";
import { ApiError } from "./ApiError";
import { HttpConstants } from "./httpConstants";
import { UiMessages } from "./uiMessages";

export interface ApiResult<T> {
  readonly ok: boolean;
  readonly httpStatus: number;
  /** The parsed JSON body, or null when the response has none. */
  readonly body: T | null;
  /** Seconds from the Retry-After header (a rate-limit answer), or null when absent. */
  readonly retryAfterSeconds: number | null;
}

async function send(
  path: string,
  options: RequestInit,
): Promise<{ response: Response; isJson: boolean }> {
  const auth = getAuth();
  const token = await auth.currentUser?.getIdToken();

  const headers = new Headers(options.headers);
  headers.set(HttpConstants.correlationIdHeaderName, crypto.randomUUID());

  if (token) {
    headers.set("Authorization", `${HttpConstants.bearerPrefix}${token}`);
  }

  const response = await fetch(`${appConfig.api.baseUrl}${path}`, {
    ...options,
    headers,
  });

  const contentType = response.headers.get("content-type");
  return { response, isJson: contentType?.includes("json") ?? false };
}

// Retry-After may also be an HTTP date; only the whole-seconds form is used.
function readRetryAfterSeconds(response: Response): number | null {
  const value = response.headers.get(HttpConstants.retryAfterHeaderName);

  if (value === null || !/^\d+$/.test(value.trim())) {
    return null;
  }

  return Number(value.trim());
}

export async function apiFetch<T>(
  path: string,
  options: RequestInit = {},
): Promise<T> {
  const { response, isJson } = await send(path, options);

  if (!response.ok) {
    const problemDetails = await response.json().catch(() => null);

    throw new ApiError(
      response.status,
      problemDetails?.title ?? UiMessages.unknownError,
      problemDetails?.detail ?? UiMessages.unexpectedError,
    );
  }

  if (isJson) {
    return response.json() as Promise<T>;
  }

  return response.text() as unknown as T;
}

// For endpoints that answer with a meaningful JSON body on 4xx too (a login that
// needs a code, a registration that is incomplete). Never throws on an HTTP status;
// only a network failure rejects.
export async function apiFetchResult<T>(
  path: string,
  options: RequestInit = {},
): Promise<ApiResult<T>> {
  const { response, isJson } = await send(path, options);
  const body = isJson
    ? ((await response.json().catch(() => null)) as T | null)
    : null;

  return {
    ok: response.ok,
    httpStatus: response.status,
    body,
    retryAfterSeconds: readRetryAfterSeconds(response),
  };
}
