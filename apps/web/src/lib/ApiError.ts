// File : /team-samsara/apps/web/src/lib/ApiError.ts
// Version : 1.0.0
// Latest commit: feature/apps-web-foundation
// Author : Gerrah
// Purpose : The one error shape every API failure is normalized into,
// mirroring the backend's ProblemDetails. No feature should handle a raw
// fetch rejection or parse a response body itself — everything goes through
// this class.

export class ApiError extends Error {
  status: number;
  title: string;
  detail: string;

  constructor(status: number, title: string, detail: string) {
    super(detail);
    this.status = status;
    this.title = title;
    this.detail = detail;
  }
}
