// File : /team-samsara/apps/web/src/lib/apiClient.ts
// Version : 1.0.0
// Latest commit: feature/apps-web-foundation
// Author : Gerrah
// Purpose : Thin fetch wrapper used by every feature's service layer.
// Attaches the current Firebase ID token and a correlation ID to every
// request, and normalizes every non-2xx response into an ApiError.

import { getAuth } from "firebase/auth";
import { ApiError } from "./ApiError";

const baseUrl = import.meta.env.VITE_API_BASE_URL;

export async function apiFetch<T>(
  path: string,
  options: RequestInit = {},
): Promise<T> {
  const auth = getAuth();
  const token = await auth.currentUser?.getIdToken();

  const headers = new Headers(options.headers);
  headers.set("X-Correlation-Id", crypto.randomUUID());

  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const response = await fetch(`${baseUrl}${path}`, {
    ...options,
    headers,
  });

  if (!response.ok) {
    const problemDetails = await response.json().catch(() => null);

    throw new ApiError(
      response.status,
      problemDetails?.title ?? "Unknown error",
      problemDetails?.detail ?? "An unexpected error occurred.",
    );
  }

  const contentType = response.headers.get("content-type");
  if (contentType?.includes("application/json")) {
    return response.json() as Promise<T>;
  }

  return response.text() as unknown as T;
}
