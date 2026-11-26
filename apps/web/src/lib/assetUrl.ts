// File : /team-samsara/apps/web/src/lib/assetUrl.ts
// Version : 1.1.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : Builds the public URL of a stored asset from its id. Images are
// streamed by the backend at GET /assets/{id}/file, so the result can be used
// directly as an <img src>. The base URL comes from appConfig.assets, which is
// the API base URL unless the environment points assets somewhere else.

import { appConfig } from "@/config/appConfig";

export function assetUrl(assetId: string): string {
  return `${appConfig.assets.baseUrl}/assets/${assetId}/file`;
}
