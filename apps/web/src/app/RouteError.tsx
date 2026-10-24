// File : /team-samsara/apps/web/src/app/RouteError.tsx
// Version : 1.0.0
// Latest commit: task/web-error-pages
// Author : Gerrah
// Purpose : Shown by the router when a page fails to load or crashes while rendering.

import { isRouteErrorResponse, useRouteError } from "react-router-dom";
import { NotFoundPage } from "@/shared/components/StatusPage/NotFoundPage";
import { ServerErrorPage } from "@/shared/components/StatusPage/ServerErrorPage";

export function RouteError() {
  const error = useRouteError();

  if (isRouteErrorResponse(error) && error.status === 404) {
    return <NotFoundPage />;
  }

  return <ServerErrorPage onRetry={() => window.location.reload()} />;
}
