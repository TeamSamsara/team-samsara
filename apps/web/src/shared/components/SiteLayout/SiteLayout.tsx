// File : /team-samsara/apps/web/src/shared/components/SiteLayout/SiteLayout.tsx
// Version : 1.4.0
// Latest commit: task/web-notifications
// Author : Gerrah
// Purpose : The page frame shared by the public and account routes: the Navbar
// on top, the sign-in banner when a member has an unread new-sign-in notice,
// the current page below it and the Footer at the bottom. On the home page the
// Navbar floats transparent over the hero, so the page starts at the very top
// of the window, unless the banner is showing. The page slides with the banner
// as it grows in and collapses.

import { useState } from "react";
import { Outlet, useMatch } from "react-router-dom";
import { useSignInAlert } from "@/features/notification/hooks/useSignInAlert";
import { SignInAlertBanner } from "@/features/notification/ui/SignInAlertBanner";
import { Footer } from "@/shared/components/Footer/Footer";
import { Navbar } from "@/shared/components/Navbar/Navbar";
import { routePaths } from "@/shared/constants/routePaths";
import styles from "./SiteLayout.module.css";

type BannerPhase = "none" | "showing" | "after";

function nextBannerPhase(phase: BannerPhase, hasBanner: boolean): BannerPhase {
  if (hasBanner) return "showing";
  return phase === "showing" ? "after" : phase;
}

function mainClassName(isHome: boolean, phase: BannerPhase): string {
  return [
    styles.main,
    isHome && styles.flush,
    phase === "showing" && styles.underBanner,
    phase === "after" && styles.afterBanner,
  ]
    .filter(Boolean)
    .join(" ");
}

export function SiteLayout() {
  const isHome = Boolean(useMatch({ path: routePaths.home, end: true }));
  const signInAlert = useSignInAlert();

  const hasBanner = signInAlert !== null;
  const [bannerPhase, setBannerPhase] = useState<BannerPhase>(
    hasBanner ? "showing" : "none",
  );

  // Adjusted while rendering, the documented way to derive state from a value,
  // so the page picks its banner classes in the same render the banner changes.
  const phase = nextBannerPhase(bannerPhase, hasBanner);
  if (phase !== bannerPhase) setBannerPhase(phase);

  // The banner sits right under the Navbar and already clears it, so while it
  // shows the page below starts at its top edge, as on the home page; the home
  // Navbar turns solid so it stays readable above the banner.
  const startsUnderNavbar = isHome && !hasBanner;

  return (
    <div className={styles.layout}>
      <Navbar transparentAtTop={startsUnderNavbar} />
      <SignInAlertBanner item={signInAlert} />
      <main className={mainClassName(isHome, phase)}>
        <Outlet />
      </main>
      <Footer />
    </div>
  );
}
