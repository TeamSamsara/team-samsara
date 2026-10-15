// File : /team-samsara/apps/web/src/vite-env.d.ts
// Version : 1.2.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : Declares the shape of import.meta.env so every VITE_* variable
// is typed. Only config/appConfig.ts reads these; the rest of the app uses appConfig.

/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** The environment this env file belongs to: development, staging or production. Must match the Vite mode. */
  readonly VITE_APP_ENV: string;
  readonly VITE_API_BASE_URL: string;
  /** Optional. Serves assets from a different API than VITE_API_BASE_URL. */
  readonly VITE_ASSETS_BASE_URL?: string;
  readonly VITE_FIREBASE_API_KEY: string;
  readonly VITE_FIREBASE_AUTH_DOMAIN: string;
  readonly VITE_FIREBASE_PROJECT_ID: string;
  readonly VITE_FIREBASE_APP_ID: string;
  /** Optional, development only. Full URL of the Firebase Auth emulator. Defaults to http://localhost:9099. Forbidden in staging and production. */
  readonly VITE_FIREBASE_AUTH_EMULATOR_HOST?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
