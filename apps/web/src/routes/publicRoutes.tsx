// File : /team-samsara/apps/web/src/routes/publicRoutes.tsx
// Version : 1.2.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : The public route tree — no auth guard. Includes the temporary
// /ping route, which proves the whole frontend foundation end to end;
// deleted once a real feature exists.

import type { RouteObject } from "react-router-dom";
import { AboutPage } from "@/features/about/view/AboutPage";
import { TeamPage } from "@/features/teams/view/TeamPage";
import { TeamsPage } from "@/features/teams/view/TeamsPage";
import { ContactPage } from "@/features/contact/view/ContactPage";
import { HomePage } from "@/features/home/view/HomePage";
import { PingPage } from "@/features/ping/view/PingPage";
import { routePaths } from "@/shared/constants/routePaths";

export const publicRoutes: RouteObject[] = [
  {
    path: routePaths.home,
    element: <HomePage />,
  },
  {
    path: routePaths.teams,
    element: <TeamsPage />,
  },
  {
    path: routePaths.team,
    element: <TeamPage />,
  },
  {
    path: routePaths.about,
    element: <AboutPage />,
  },
  {
    path: routePaths.contact,
    element: <ContactPage />,
  },
  {
    path: routePaths.ping,
    element: <PingPage />,
  },
];
