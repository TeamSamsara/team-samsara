// File : /team-samsara/apps/web/src/routes/accountRoutes.tsx
// Version : 1.4.0
// Latest commit: task/web-account-security
// Author : Gerrah
// Purpose : The authenticated customer route tree, gated by RouteGuard at
// member access level or above: the account home, the edit-profile page, the
// account security overview and the change-password and change-email pages.

import type { RouteObject } from "react-router-dom";
import { RouteGuard } from "@/shared/components/RouteGuard/RouteGuard";
import { routePaths } from "@/shared/constants/routePaths";
import { AccountPlaceholder } from "@/features/account/view/AccountPlaceholder";
import { ChangeEmailPage } from "@/features/account/view/ChangeEmailPage";
import { ChangePasswordPage } from "@/features/account/view/ChangePasswordPage";
import { ProfileSettingsPage } from "@/features/account/view/ProfileSettingsPage";
import { SecurityOverviewPage } from "@/features/account/view/SecurityOverviewPage";

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
  {
    path: routePaths.accountSecurity,
    element: (
      <RouteGuard requiredAccessLevel="member">
        <SecurityOverviewPage />
      </RouteGuard>
    ),
  },
  {
    path: routePaths.accountSecurityPassword,
    element: (
      <RouteGuard requiredAccessLevel="member">
        <ChangePasswordPage />
      </RouteGuard>
    ),
  },
  {
    path: routePaths.accountSecurityEmail,
    element: (
      <RouteGuard requiredAccessLevel="member">
        <ChangeEmailPage />
      </RouteGuard>
    ),
  },
];
