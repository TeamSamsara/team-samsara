// File : /team-samsara/apps/web/src/routes/publicRoutes.tsx
// Version : 1.0.0
// Latest commit: feature/apps-web-foundation
// Author : Gerrah
// Purpose : The public route tree — no auth guard. Includes the temporary
// /ping route, which proves the whole frontend foundation end to end;
// deleted once a real feature exists.

import type { RouteObject } from "react-router-dom";
import { PingPage } from "@/features/ping/view/PingPage";
import { routePaths } from "@/shared/constants/routePaths";

export const publicRoutes: RouteObject[] = [
  {
    path: routePaths.ping,
    element: <PingPage />,
  },
];
