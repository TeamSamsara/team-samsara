// File : /team-samsara/apps/web/src/app/ErrorBoundary.tsx
// Version : 1.0.1
// Latest commit: feature/string-magic-value-conventions
// Author : Gerrah
// Purpose : Catches render-time failures anywhere in the app.

import {
  ErrorBoundary as ReactErrorBoundary,
  type FallbackProps,
} from "react-error-boundary";
import type { ReactNode } from "react";
import { UiMessages } from "@/lib/uiMessages";

function ErrorFallback({ error }: FallbackProps) {
  const message =
    error instanceof Error ? error.message : UiMessages.unexpectedError;

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
