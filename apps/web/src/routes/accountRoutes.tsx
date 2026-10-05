// File : /team-samsara/apps/web/src/routes/accountRoutes.tsx
// Version : 1.0.1
// Latest commit: feature/apps-web-foundation
// Author : Gerrah
// Purpose : The authenticated customer route tree, gated by RouteGuard at
// Member access level or above. AccountPlaceholder now lives in its own
// file (features/account/view).

import type { RouteObject } from "react-router-dom";
import { RouteGuard } from "@/shared/components/RouteGuard";
import { routePaths } from "@/shared/constants/routePaths";
import { AccountPlaceholder } from "@/features/account/view/AccountPlaceholder";

export const accountRoutes: RouteObject[] = [
  {
    path: routePaths.account,
    element: (
      <RouteGuard requiredAccessLevel="Member">
        <AccountPlaceholder />
      </RouteGuard>
    ),
  },
];
