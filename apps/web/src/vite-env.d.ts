// File : /team-samsara/apps/web/src/vite-env.d.ts
// Version : 1.1.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : Declares the shape of import.meta.env so every VITE_* variable
// used in the app is properly typed instead of implicitly any.

/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_FIREBASE_API_KEY: string;
  readonly VITE_FIREBASE_AUTH_DOMAIN: string;
  readonly VITE_FIREBASE_PROJECT_ID: string;
  readonly VITE_FIREBASE_APP_ID: string;
  readonly VITE_API_BASE_URL: string;
  /** Optional. Serves assets from a different API than VITE_API_BASE_URL. */
  readonly VITE_ASSETS_BASE_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
