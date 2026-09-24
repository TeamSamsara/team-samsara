// File : /team-samsara/apps/web/src/lib/httpConstants.ts
// Version : 1.0.0
// Latest commit: feature/string-magic-value-conventions
// Author : Gerrah
// Purpose : HTTP protocol values our code depends on matching exactly. Mirrors the
// backend's HttpConstants.cs - CorrelationIdHeaderName in particular must match the
// backend's CorrelationIdMiddleware.HeaderName exactly, or correlation IDs silently stop
// linking frontend and backend logs together.

export const HttpConstants = {
  correlationIdHeaderName: "X-Correlation-Id",
  bearerPrefix: "Bearer ",
} as const;
