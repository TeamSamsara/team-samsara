// File : /team-samsara/apps/web/src/features/home/view/HomePage.tsx
// Version : 1.2.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The home page: the hero, then the recent updates grid and the
// recent content row. The footer is added next.

import { HomeHero } from "../ui/HomeHero";
import { RecentContent } from "../ui/RecentContent";
import { RecentUpdates } from "../ui/RecentUpdates";

export function HomePage() {
  return (
    <>
      <HomeHero />
      <RecentUpdates />
      <RecentContent />
    </>
  );
}
