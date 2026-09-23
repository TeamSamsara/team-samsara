// File : /team-samsara/apps/web/src/app/ErrorBoundary.tsx
// Version : 1.0.0
// Latest commit: feature/apps-web-foundation
// Author : Gerrah
// Purpose : Catches render-time failures anywhere in the app. Wraps
// react-error-boundary rather than hand-rolling a class component, since
// React still requires boundaries to be classes internally either way.
// FallbackProps.error is typed as unknown (JS allows throwing non-Error
// values), so it's narrowed before .message is accessed.

import {
  ErrorBoundary as ReactErrorBoundary,
  type FallbackProps,
} from "react-error-boundary";
import type { ReactNode } from "react";

function ErrorFallback({ error }: FallbackProps) {
  const message =
    error instanceof Error ? error.message : "An unexpected error occurred.";

  return (
    <div role="alert">
      <h1>Something went wrong.</h1>
      <p>{message}</p>
    </div>
  );
}

export function ErrorBoundary({ children }: { children: ReactNode }) {
  return (
    <ReactErrorBoundary FallbackComponent={ErrorFallback}>
      {children}
    </ReactErrorBoundary>
  );
}
