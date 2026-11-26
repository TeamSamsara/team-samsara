// File : /team-samsara/apps/web/src/routes/accountRoutes.tsx
// Version : 1.5.0
// Latest commit: task/web-member-profile
// Author : Gerrah
// Purpose : The authenticated customer route tree, gated by RouteGuard at
// member access level or above: the account home, a member's profile page (the
// owner edits it there), the old edit-profile address that redirects to it, the
// account security overview and the change-password and change-email pages.

import type { RouteObject } from "react-router-dom";
import { RouteGuard } from "@/shared/components/RouteGuard/RouteGuard";
import { routePaths } from "@/shared/constants/routePaths";
import { AccountPlaceholder } from "@/features/account/view/AccountPlaceholder";
import { ChangeEmailPage } from "@/features/account/view/ChangeEmailPage";
import { ChangePasswordPage } from "@/features/account/view/ChangePasswordPage";
import { MemberProfilePage } from "@/features/account/view/MemberProfilePage";
import { MyProfileRedirect } from "@/features/account/view/MyProfileRedirect";
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
        <MyProfileRedirect />
      </RouteGuard>
    ),
  },
  {
    path: routePaths.memberProfile,
    element: (
      <RouteGuard requiredAccessLevel="member">
        <MemberProfilePage />
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
