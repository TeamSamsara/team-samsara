// File : /team-samsara/apps/web/src/routes/authRoutes.tsx
// Version : 1.3.0
// Latest commit: task/web-auth-codes-reset
// Author : Gerrah
// Purpose : The auth route tree: Register, Log in, their code screens and
// Forgot password. These screens have no navbar or footer, so they sit
// outside SiteLayout.

import type { RouteObject } from "react-router-dom";
import { ForgotPasswordPage } from "@/features/auth/view/ForgotPasswordPage";
import { LoginPage } from "@/features/auth/view/LoginPage";
import { LoginVerifyPage } from "@/features/auth/view/LoginVerifyPage";
import { RegisterPage } from "@/features/auth/view/RegisterPage";
import { RegisterVerifyPage } from "@/features/auth/view/RegisterVerifyPage";
import { routePaths } from "@/shared/constants/routePaths";

export const authRoutes: RouteObject[] = [
  {
    path: routePaths.register,
    element: <RegisterPage />,
  },
  {
    path: routePaths.registerVerify,
    element: <RegisterVerifyPage />,
  },
  {
    path: routePaths.login,
    element: <LoginPage />,
  },
  {
    path: routePaths.loginVerify,
    element: <LoginVerifyPage />,
  },
  {
    path: routePaths.forgotPassword,
    element: <ForgotPasswordPage />,
  },
];
