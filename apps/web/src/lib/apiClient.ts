// File : /team-samsara/apps/web/src/lib/apiClient.ts
// Version : 1.0.1
// Latest commit: feature/string-magic-value-conventions
// Author : Gerrah
// Purpose : Thin fetch wrapper used by every feature's service layer.

import { getAuth } from "firebase/auth";
import { ApiError } from "./ApiError";
import { HttpConstants } from "./httpConstants";
import { UiMessages } from "./uiMessages";

const baseUrl = import.meta.env.VITE_API_BASE_URL;

export async function apiFetch<T>(
  path: string,
  options: RequestInit = {},
): Promise<T> {
  const auth = getAuth();
  const token = await auth.currentUser?.getIdToken();

  const headers = new Headers(options.headers);
  headers.set(HttpConstants.correlationIdHeaderName, crypto.randomUUID());

  if (token) {
    headers.set("Authorization", `${HttpConstants.bearerPrefix}${token}`);
  }

  const response = await fetch(`${baseUrl}${path}`, {
    ...options,
    headers,
  });

  if (!response.ok) {
    const problemDetails = await response.json().catch(() => null);

    throw new ApiError(
      response.status,
      problemDetails?.title ?? UiMessages.unknownError,
      problemDetails?.detail ?? UiMessages.unexpectedError,
    );
  }

  const contentType = response.headers.get("content-type");
  if (contentType?.includes("application/json")) {
    return response.json() as Promise<T>;
  }

  return response.text() as unknown as T;
}
