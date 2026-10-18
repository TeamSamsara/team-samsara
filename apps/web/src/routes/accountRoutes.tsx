// File : /team-samsara/apps/web/src/routes/accountRoutes.tsx
// Version : 1.1.0
// Latest commit: task/web-account-settings
// Author : Gerrah
// Purpose : The authenticated customer route tree, gated by RouteGuard at
// member access level or above: the account home and the edit-profile page.

import type { RouteObject } from "react-router-dom";
import { RouteGuard } from "@/shared/components/RouteGuard/RouteGuard";
import { routePaths } from "@/shared/constants/routePaths";
import { AccountPlaceholder } from "@/features/account/view/AccountPlaceholder";
import { ProfileSettingsPage } from "@/features/account/view/ProfileSettingsPage";

export const accountRoutes: RouteObject[] = [
  {
    path: routePaths.account,
    element: (
      <RouteGuard requiredAccessLevel="member">
        <AccountPlaceholder />
      </RouteGuard>
    ),
  },
  {
    path: routePaths.accountProfile,
    element: (
      <RouteGuard requiredAccessLevel="member">
        <ProfileSettingsPage />
      </RouteGuard>
    ),
  },
];
