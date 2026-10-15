// File : /team-samsara/apps/web/src/features/auth/service/accountOutcome.ts
// Version : 1.0.0
// Latest commit: task/web-auth-foundation
// Author : Gerrah
// Purpose : The boundary between the account API and the app. Every answer becomes one
// ApiOutcome: a recognized body, or an explicit rate-limited, unauthorized, forbidden,
// unknown or unavailable. An unrecognized value is never read as success.

import { apiFetchResult } from "@/lib/apiClient";
import {
  accessLevels,
  authStates,
  passwordStatuses,
  type AccountSummary,
  type AuthState,
  type CodeSentResult,
  type PasswordResetTokenResult,
} from "../types/authTypes";

const HttpStatus = {
  unauthorized: 401,
  forbidden: 403,
  tooManyRequests: 429,
} as const;

const jsonHeaders = { "Content-Type": "application/json" } as const;

export type ApiOutcome<T> =
  /** The API gave a body this app recognizes (its status may still be a failure). */
  | { readonly kind: "answer"; readonly value: T }
  /** Too many requests from this address: wait, then try again. */
  | { readonly kind: "rateLimited"; readonly retryAfterSeconds: number | null }
  /** The caller is not identified (no token, or an invalid one). */
  | { readonly kind: "unauthorized" }
  /** The caller is known but not allowed yet; authState says why, when the API says. */
  | { readonly kind: "forbidden"; readonly authState: AuthState | null }
  /** The API answered something this app does not recognize. */
  | { readonly kind: "unknown" }
  /** The API could not be reached. */
  | { readonly kind: "unavailable" };

// Reads a response body into T, or returns null when the body is not recognized.
type Parser<T> = (body: unknown, ok: boolean) => T | null;

type JsonObject = Record<string, unknown>;

function isObject(value: unknown): value is JsonObject {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function isOneOf<T extends string>(
  list: readonly T[],
  value: unknown,
): value is T {
  return (
    typeof value === "string" && (list as readonly string[]).includes(value)
  );
}

function isWholeSeconds(value: unknown): value is number {
  return typeof value === "number" && Number.isInteger(value) && value >= 0;
}

// ---- Parsers ----

export function parseStatus<TStatus extends string>(
  statuses: readonly TStatus[],
): Parser<{ readonly status: TStatus }> {
  return (body) => {
    if (!isObject(body) || !isOneOf(statuses, body.status)) {
      return null;
    }

    return { status: body.status };
  };
}

export function parseCodeSent<TStatus extends string>(
  statuses: readonly TStatus[],
): Parser<CodeSentResult<TStatus>> {
  return (body) => {
    if (
      !isObject(body) ||
      !isOneOf(statuses, body.status) ||
      !isWholeSeconds(body.codeLength) ||
      !isWholeSeconds(body.retryAfterSeconds)
    ) {
      return null;
    }

    return {
      status: body.status,
      codeLength: body.codeLength,
      retryAfterSeconds: body.retryAfterSeconds,
    };
  };
}

export const parseResetToken: Parser<PasswordResetTokenResult> = (body) => {
  if (!isObject(body) || !isOneOf(passwordStatuses, body.status)) {
    return null;
  }

  const token = typeof body.resetToken === "string" ? body.resetToken : null;

  // A success without a token cannot be used to continue, so it is not trusted.
  if (body.status === "success" && token === null) {
    return null;
  }

  return { status: body.status, resetToken: token };
};

export const parseAccountSummary: Parser<AccountSummary> = (body) => {
  if (
    !isObject(body) ||
    typeof body.userId !== "string" ||
    !isOneOf(accessLevels, body.accessLevel) ||
    !isOneOf(authStates, body.authState)
  ) {
    return null;
  }

  return {
    userId: body.userId,
    accessLevel: body.accessLevel,
    authState: body.authState,
  };
};

// For calls that answer 204 with no body: only a successful response counts.
export const parseNoContent: Parser<true> = (_body, ok) => (ok ? true : null);

// ---- Requests ----

export function postJson(payload: object): RequestInit {
  return {
    method: "POST",
    headers: jsonHeaders,
    body: JSON.stringify(payload),
  };
}

export const postEmpty: RequestInit = { method: "POST" };

// Sends the request and turns whatever comes back into one ApiOutcome.
export async function sendAccountRequest<T>(
  path: string,
  parse: Parser<T>,
  options: RequestInit = {},
): Promise<ApiOutcome<T>> {
  let result;

  try {
    result = await apiFetchResult<unknown>(path, options);
  } catch {
    return { kind: "unavailable" };
  }

  // The body's own status is authoritative, whatever the HTTP code.
  const value = parse(result.body, result.ok);

  if (value !== null) {
    return { kind: "answer", value };
  }

  switch (result.httpStatus) {
    case HttpStatus.tooManyRequests:
      return {
        kind: "rateLimited",
        retryAfterSeconds: result.retryAfterSeconds,
      };
    case HttpStatus.unauthorized:
      return { kind: "unauthorized" };
    case HttpStatus.forbidden:
      return { kind: "forbidden", authState: readAuthState(result.body) };
    default:
      return { kind: "unknown" };
  }
}

function readAuthState(body: unknown): AuthState | null {
  return isObject(body) && isOneOf(authStates, body.authState)
    ? body.authState
    : null;
}
