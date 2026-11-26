// File : /team-samsara/apps/web/src/config/appConfig.ts
// Version : 1.0.1
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The only file that reads import.meta.env. Resolves which environment
// the app runs in, validates every value as a whole and exposes one typed appConfig.
// Services import appConfig and never read env variables or the Vite mode themselves.

export const appEnvironments = [
  "development",
  "staging",
  "production",
] as const;

export type AppEnvironment = (typeof appEnvironments)[number];

export interface AppConfig {
  readonly environment: AppEnvironment;
  readonly api: {
    readonly baseUrl: string;
  };
  readonly assets: {
    readonly baseUrl: string;
  };
  readonly firebase: {
    readonly apiKey: string;
    readonly authDomain: string;
    readonly projectId: string;
    readonly appId: string;
    /** Only set in the development environment. Always null in staging and production. */
    readonly authEmulatorUrl: string | null;
  };
}

export class AppConfigError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "AppConfigError";
  }
}

const defaultAuthEmulatorUrl = "http://localhost:9099";

const ConfigMessages = {
  header: (mode: string) =>
    `Invalid application configuration (Vite mode: "${mode}"):`,
  unknownMode: (mode: string) =>
    `Unknown Vite mode "${mode}". Run Vite with --mode development, --mode staging or --mode production.`,
  missing: (key: string) => `${key} is required but missing or empty.`,
  modeMismatch: (mode: string, declared: string) =>
    `VITE_APP_ENV is "${declared}" but the Vite mode is "${mode}". The env file and the mode must name the same environment.`,
  notAbsoluteUrl: (key: string) =>
    `${key} must be an absolute http(s) URL, for example https://example.com.`,
  needsHttps: (key: string, environment: string) =>
    `${key} must use https in the ${environment} environment.`,
  emulatorForbidden: (environment: string) =>
    `VITE_FIREBASE_AUTH_EMULATOR_HOST is set, but the Auth emulator is only allowed in the development environment, not in ${environment}.`,
  developmentBuild:
    "The development environment cannot be built. Build with --mode staging or --mode production.",
} as const;

function isAppEnvironment(value: string): value is AppEnvironment {
  return (appEnvironments as readonly string[]).includes(value);
}

function readText(
  value: string | undefined,
  key: string,
  problems: string[],
): string {
  const text = (value ?? "").trim();
  if (!text) {
    problems.push(ConfigMessages.missing(key));
  }
  return text;
}

// Validates an absolute http(s) URL and returns it without trailing slashes.
// Anything other than the development environment must use https.
// In development only, a root-relative path such as /api is also accepted: it is
// the Vite dev server proxy to the local API, and never exists in a built bundle.
function readUrl(
  value: string | undefined,
  key: string,
  environment: AppEnvironment,
  problems: string[],
): string {
  const text = readText(value, key, problems);
  if (!text) return "";

  if (environment === "development" && isRootRelativePath(text)) {
    return text.replace(/\/+$/, "");
  }

  return checkUrl(text, key, environment, problems);
}

function isRootRelativePath(text: string): boolean {
  return text.startsWith("/") && !text.startsWith("//");
}

function checkUrl(
  text: string,
  key: string,
  environment: AppEnvironment,
  problems: string[],
): string {
  let url: URL;
  try {
    url = new URL(text);
  } catch {
    problems.push(ConfigMessages.notAbsoluteUrl(key));
    return "";
  }

  if (url.protocol !== "http:" && url.protocol !== "https:") {
    problems.push(ConfigMessages.notAbsoluteUrl(key));
    return "";
  }

  if (environment !== "development" && url.protocol !== "https:") {
    problems.push(ConfigMessages.needsHttps(key, environment));
  }

  return text.replace(/\/+$/, "");
}

export function resolveAppConfig(env: ImportMetaEnv): AppConfig {
  // The environment is chosen by the Vite mode (--mode), and nothing else.
  // import.meta.env.DEV and PROD only say whether this is the dev server or a built bundle,
  // so they are never used to pick an environment.
  const mode = env.MODE;
  if (!isAppEnvironment(mode)) {
    throw new AppConfigError(
      `${ConfigMessages.header(mode)}\n - ${ConfigMessages.unknownMode(mode)}`,
    );
  }
  const environment = mode;
  const problems: string[] = [];

  // Each env file declares its own environment, so a wrong file or a wrong mode is caught.
  const declared = readText(env.VITE_APP_ENV, "VITE_APP_ENV", problems);
  if (declared && declared !== environment) {
    problems.push(ConfigMessages.modeMismatch(environment, declared));
  }

  // A built bundle can never be the development environment (it would carry the emulator).
  if (env.PROD && environment === "development") {
    problems.push(ConfigMessages.developmentBuild);
  }

  const apiBaseUrl = readUrl(
    env.VITE_API_BASE_URL,
    "VITE_API_BASE_URL",
    environment,
    problems,
  );

  // Assets come from the same API unless this environment's file says otherwise.
  const rawAssetsBaseUrl = (env.VITE_ASSETS_BASE_URL ?? "").trim();
  const assetsBaseUrl = rawAssetsBaseUrl
    ? checkUrl(rawAssetsBaseUrl, "VITE_ASSETS_BASE_URL", environment, problems)
    : apiBaseUrl;

  const firebaseApiKey = readText(
    env.VITE_FIREBASE_API_KEY,
    "VITE_FIREBASE_API_KEY",
    problems,
  );
  const firebaseAuthDomain = readText(
    env.VITE_FIREBASE_AUTH_DOMAIN,
    "VITE_FIREBASE_AUTH_DOMAIN",
    problems,
  );
  const firebaseProjectId = readText(
    env.VITE_FIREBASE_PROJECT_ID,
    "VITE_FIREBASE_PROJECT_ID",
    problems,
  );
  const firebaseAppId = readText(
    env.VITE_FIREBASE_APP_ID,
    "VITE_FIREBASE_APP_ID",
    problems,
  );

  // The Auth emulator exists in the development environment only.
  const rawEmulatorUrl = (env.VITE_FIREBASE_AUTH_EMULATOR_HOST ?? "").trim();
  let authEmulatorUrl: string | null = null;
  if (environment === "development") {
    authEmulatorUrl =
      checkUrl(
        rawEmulatorUrl || defaultAuthEmulatorUrl,
        "VITE_FIREBASE_AUTH_EMULATOR_HOST",
        environment,
        problems,
      ) || null;
  } else if (rawEmulatorUrl) {
    problems.push(ConfigMessages.emulatorForbidden(environment));
  }

  if (problems.length > 0) {
    const lines = problems.map((problem) => ` - ${problem}`).join("\n");
    throw new AppConfigError(`${ConfigMessages.header(mode)}\n${lines}`);
  }

  return Object.freeze({
    environment,
    api: Object.freeze({ baseUrl: apiBaseUrl }),
    assets: Object.freeze({ baseUrl: assetsBaseUrl }),
    firebase: Object.freeze({
      apiKey: firebaseApiKey,
      authDomain: firebaseAuthDomain,
      projectId: firebaseProjectId,
      appId: firebaseAppId,
      authEmulatorUrl,
    }),
  });
}

export const appConfig: AppConfig = resolveAppConfig(import.meta.env);
