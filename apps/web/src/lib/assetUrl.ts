// File : /team-samsara/apps/web/src/lib/assetUrl.ts
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : Builds the public URL of a stored asset from its id. Images are
// streamed by the backend at GET /assets/{id}/file, so the result can be used
// directly as an <img src>. VITE_ASSETS_BASE_URL optionally points assets at a
// different API than VITE_API_BASE_URL (for example staging images while the
// local API has none); when empty or unset, the regular API base URL is used.

const rawBaseUrl =
  import.meta.env.VITE_ASSETS_BASE_URL || import.meta.env.VITE_API_BASE_URL;

const assetsBaseUrl = rawBaseUrl.replace(/\/+$/, "");

export function assetUrl(assetId: string): string {
  return `${assetsBaseUrl}/assets/${assetId}/file`;
}
