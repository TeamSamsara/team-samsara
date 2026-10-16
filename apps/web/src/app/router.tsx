// File : /team-samsara/apps/web/src/app/router.tsx
// Version : 1.2.0
// Latest commit: task/web-auth-screens
// Author : Gerrah
// Purpose : Composes the route trees into one router, provided to
// RouterProvider in main.tsx. The public and account trees share SiteLayout
// (Navbar on top); the auth and CMS trees sit outside it.

import { createBrowserRouter, type RouteObject } from "react-router-dom";
import { publicRoutes } from "@/routes/publicRoutes";
import { accountRoutes } from "@/routes/accountRoutes";
import { authRoutes } from "@/routes/authRoutes";
import { cmsRoutes } from "@/routes/cmsRoutes";
import { SiteLayout } from "@/shared/components/SiteLayout/SiteLayout";

const routes: RouteObject[] = [
  { element: <SiteLayout />, children: [...publicRoutes, ...accountRoutes] },
  ...authRoutes,
  ...cmsRoutes,
];

export const router = createBrowserRouter(routes);
