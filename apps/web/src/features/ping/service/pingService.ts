// File : /team-samsara/apps/web/src/features/ping/service/pingService.ts
// Version : 1.0.0
// Latest commit: feature/apps-web-foundation
// Author : Gerrah
// Purpose : Calls the backend's /ping endpoint. Thin wrapper around
// apiFetch — the query hook (usePing) calls this rather than apiFetch
// directly, keeping the feature's data-access shape consistent with how
// every future feature's service layer will look.

import { apiFetch } from "@/lib/apiClient";

export function fetchPing(): Promise<string> {
  return apiFetch<string>("/ping");
}
