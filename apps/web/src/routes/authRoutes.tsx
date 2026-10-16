// File : /team-samsara/apps/web/src/routes/authRoutes.tsx
// Version : 1.0.0
// Latest commit: task/web-auth-screens
// Author : Gerrah
// Purpose : The auth route tree: Register and Log in. These screens have no
// navbar or footer, so they sit outside SiteLayout.

import type { RouteObject } from "react-router-dom";
import { LoginPage } from "@/features/auth/view/LoginPage";
import { RegisterPage } from "@/features/auth/view/RegisterPage";
import { routePaths } from "@/shared/constants/routePaths";

export const authRoutes: RouteObject[] = [
  {
    path: routePaths.register,
    element: <RegisterPage />,
  },
  {
    path: routePaths.login,
    element: <LoginPage />,
  },
];
