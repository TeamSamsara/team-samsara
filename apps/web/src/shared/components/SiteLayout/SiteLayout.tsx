// File : /team-samsara/apps/web/src/shared/components/SiteLayout/SiteLayout.tsx
// Version : 1.1.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The page frame shared by the public and account routes: the Navbar
// on top and the current page below it. On the home page the Navbar floats
// transparent over the hero, so the page starts at the very top of the window.

import { Outlet, useMatch } from "react-router-dom";
import { Navbar } from "@/shared/components/Navbar/Navbar";
import { routePaths } from "@/shared/constants/routePaths";
import styles from "./SiteLayout.module.css";

export function SiteLayout() {
  const isHome = Boolean(useMatch({ path: routePaths.home, end: true }));

  return (
    <>
      <Navbar transparentAtTop={isHome} />
      <main className={isHome ? `${styles.main} ${styles.flush}` : styles.main}>
        <Outlet />
      </main>
    </>
  );
}
