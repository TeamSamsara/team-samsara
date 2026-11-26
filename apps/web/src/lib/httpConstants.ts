// File : /team-samsara/apps/web/src/lib/httpConstants.ts
// Version : 1.1.0
// Latest commit: task/web-auth-foundation
// Author : Gerrah
// Purpose : HTTP protocol values our code depends on matching exactly. Mirrors the
// backend's HttpConstants.cs - CorrelationIdHeaderName in particular must match the
// backend's CorrelationIdMiddleware.HeaderName exactly, or correlation IDs silently stop
// linking frontend and backend logs together.

export const HttpConstants = {
  correlationIdHeaderName: "X-Correlation-Id",
  retryAfterHeaderName: "Retry-After",
  bearerPrefix: "Bearer ",
} as const;
