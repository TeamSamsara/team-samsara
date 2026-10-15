// File : /team-samsara/apps/web/src/routes/publicRoutes.tsx
// Version : 1.1.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The public route tree — no auth guard. Includes the temporary
// /ping route, which proves the whole frontend foundation end to end;
// deleted once a real feature exists.

import type { RouteObject } from "react-router-dom";
import { HomePage } from "@/features/home/view/HomePage";
import { PingPage } from "@/features/ping/view/PingPage";
import { routePaths } from "@/shared/constants/routePaths";

export const publicRoutes: RouteObject[] = [
  {
    path: routePaths.home,
    element: <HomePage />,
  },
  {
    path: routePaths.ping,
    element: <PingPage />,
  },
];
