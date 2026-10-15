// File : /team-samsara/apps/web/src/app/router.tsx
// Version : 1.1.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : Composes the three route trees into one router, provided to
// RouterProvider in main.tsx. The public and account trees share SiteLayout
// (Navbar on top); the CMS tree sits outside it.

import { createBrowserRouter, type RouteObject } from "react-router-dom";
import { publicRoutes } from "@/routes/publicRoutes";
import { accountRoutes } from "@/routes/accountRoutes";
import { cmsRoutes } from "@/routes/cmsRoutes";
import { SiteLayout } from "@/shared/components/SiteLayout/SiteLayout";

const routes: RouteObject[] = [
  { element: <SiteLayout />, children: [...publicRoutes, ...accountRoutes] },
  ...cmsRoutes,
];

export const router = createBrowserRouter(routes);
