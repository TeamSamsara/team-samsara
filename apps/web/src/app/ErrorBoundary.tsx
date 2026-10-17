// File : /team-samsara/apps/web/src/app/ErrorBoundary.tsx
// Version : 2.0.0
// Latest commit: task/web-error-pages
// Author : Gerrah
// Purpose : Last safety net: catches render failures outside the router and shows the 500 page.

import { ErrorBoundary as ReactErrorBoundary } from "react-error-boundary";
import type { ReactNode } from "react";
import { Button } from "@/shared/components/Button/Button";
import { errorPagesContent } from "@/shared/components/StatusPage/errorPagesContent";
import { StatusPage } from "@/shared/components/StatusPage/StatusPage";

function ErrorFallback() {
  const text = errorPagesContent.serverError;

  return (
    <StatusPage
      code={text.code}
      title={text.title}
      body={text.body}
      actions={
        <Button onClick={() => window.location.reload()}>{text.retry}</Button>
      }
    />
  );
}

export function ErrorBoundary({ children }: { children: ReactNode }) {
  return (
    <ReactErrorBoundary FallbackComponent={ErrorFallback}>
      {children}
    </ReactErrorBoundary>
  );
}
