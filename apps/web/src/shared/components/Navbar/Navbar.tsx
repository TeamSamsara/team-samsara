// File : /team-samsara/apps/web/src/shared/components/Navbar/Navbar.tsx
// Version : 1.5.0
// Latest commit: task/web-cleanup-and-glow
// Author : Gerrah
// Purpose : The fixed top navigation bar shown on every page: transparent over
// a hero until the page scrolls, then solid. Shows Sign in / Log in for guests
// and the notification bell and account menu for signed-in members. While the
// session is still being checked it draws the member layout if the last visit
// was a member, so a refresh does not flash the guest buttons. The background
// fade only starts once the page has settled, so a refresh while scrolled does
// not fade the bar in from transparent.

import { useState } from "react";
import { Link, NavLink } from "react-router-dom";
import { useAuth } from "@/app/useAuth";
import { NotificationBell } from "@/features/notification/ui/NotificationBell";
import { readAuthHint } from "@/lib/authHint";
import { routePaths } from "@/shared/constants/routePaths";
import { useScrollPast } from "@/shared/hooks/useScrollPast";
import { useSettled } from "@/shared/hooks/useSettled";
import {
  NavbarText,
  navbarLinks,
  navbarFadeStartsAfterMs,
  navbarSolidAfterScrollY,
} from "./navbarContent";
import { NavbarAccount } from "./NavbarAccount";
import { NavbarLanguage } from "./NavbarLanguage";
import styles from "./Navbar.module.css";

interface NavbarProps {
  transparentAtTop?: boolean;
}

export function Navbar({ transparentAtTop = false }: NavbarProps) {
  const { status } = useAuth();
  const hasScrolled = useScrollPast(navbarSolidAfterScrollY);
  const canFade = useSettled(navbarFadeStartsAfterMs);
  const isSolid = !transparentAtTop || hasScrolled;

  // Read once, on the first render: it only covers the moment before the real answer.
  const [wasMember] = useState(() => readAuthHint() !== null);
  const showsMember =
    status === "member" || (status === "loading" && wasMember);

  return (
    <header
      className={`${styles.navbar} ${isSolid ? styles.solid : ""} ${canFade ? styles.fades : ""}`}
    >
      <nav className={styles.links} aria-label={NavbarText.mainNavigation}>
        {navbarLinks.map((link) => (
          <NavLink
            key={link.to}
            to={link.to}
            end={link.to === routePaths.home}
            className={({ isActive }) =>
              `${styles.link} ${isActive ? styles.linkActive : ""}`
            }
          >
            {link.label}
          </NavLink>
        ))}
      </nav>

      <div className={styles.actions}>
        <NavbarLanguage />

        {showsMember ? (
          <>
            <NotificationBell />
            <NavbarAccount />
          </>
        ) : (
          <div className={styles.buttons}>
            <Link
              to={routePaths.register}
              className={`${styles.cta} ${styles.ctaOutline}`}
            >
              {NavbarText.signIn}
            </Link>
            <Link
              to={routePaths.login}
              className={`${styles.cta} ${styles.ctaFilled}`}
            >
              {NavbarText.logIn}
            </Link>
          </div>
        )}
      </div>
    </header>
  );
}
