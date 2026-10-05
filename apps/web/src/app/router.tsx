// File : /team-samsara/apps/web/src/app/router.tsx
// Version : 1.0.0
// Latest commit: feature/apps-web-foundation
// Author : Gerrah
// Purpose : Composes the three route trees into one router, provided to
// RouterProvider in main.tsx.

import { createBrowserRouter, type RouteObject } from "react-router-dom";
import { publicRoutes } from "@/routes/publicRoutes";
import { accountRoutes } from "@/routes/accountRoutes";
import { cmsRoutes } from "@/routes/cmsRoutes";

const routes: RouteObject[] = [...publicRoutes, ...accountRoutes, ...cmsRoutes];

export const router = createBrowserRouter(routes);
