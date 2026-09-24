// File : /team-samsara/apps/web/src/features/ping/service/pingService.ts
// Version : 1.0.1
// Latest commit: feature/string-magic-value-conventions
// Author : Gerrah
// Purpose : Calls the backend's /ping endpoint.

import { apiFetch } from "@/lib/apiClient";
import { PingApiRoutes } from "../constants/pingApiRoutes";

export function fetchPing(): Promise<string> {
  return apiFetch<string>(PingApiRoutes.ping);
}
