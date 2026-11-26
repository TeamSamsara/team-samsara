// File : /team-samsara/apps/web/src/routes/cmsRoutes.tsx
// Version : 1.0.2
// Latest commit: task/web-auth-foundation
// Author : Gerrah
// Purpose : The CMS admin route tree, gated by RouteGuard at admin access
// level. CmsPlaceholder now lives in its own file (features/cms/view).

import type { RouteObject } from "react-router-dom";
import { RouteGuard } from "@/shared/components/RouteGuard/RouteGuard";
import { routePaths } from "@/shared/constants/routePaths";
import { CmsPlaceholder } from "@/features/cms/view/CmsPlaceholder";

export const cmsRoutes: RouteObject[] = [
  {
    path: routePaths.cms,
    element: (
      <RouteGuard requiredAccessLevel="admin">
        <CmsPlaceholder />
      </RouteGuard>
    ),
  },
];
