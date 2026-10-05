// File : /team-samsara/apps/web/src/routes/cmsRoutes.tsx
// Version : 1.0.1
// Latest commit: feature/apps-web-foundation
// Author : Gerrah
// Purpose : The CMS admin route tree, gated by RouteGuard at Admin access
// level. CmsPlaceholder now lives in its own file (features/cms/view).

import type { RouteObject } from "react-router-dom";
import { RouteGuard } from "@/shared/components/RouteGuard";
import { routePaths } from "@/shared/constants/routePaths";
import { CmsPlaceholder } from "@/features/cms/view/CmsPlaceholder";

export const cmsRoutes: RouteObject[] = [
  {
    path: routePaths.cms,
    element: (
      <RouteGuard requiredAccessLevel="Admin">
        <CmsPlaceholder />
      </RouteGuard>
    ),
  },
];
