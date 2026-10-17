// File : /team-samsara/apps/web/src/shared/components/Navbar/Navbar.tsx
// Version : 1.2.0
// Latest commit: task/web-auth-onboarding-navbar
// Author : Gerrah
// Purpose : The fixed top navigation bar shown on every page: transparent over
// a hero until the page scrolls, then solid. Shows Sign in / Log in for guests
// and the account menu for signed-in members.

import { Link, NavLink } from "react-router-dom";
import { useAuth } from "@/app/useAuth";
import { routePaths } from "@/shared/constants/routePaths";
import { useScrollPast } from "@/shared/hooks/useScrollPast";
import {
  NavbarText,
  navbarLinks,
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
  const isSolid = !transparentAtTop || hasScrolled;

  return (
    <header className={`${styles.navbar} ${isSolid ? styles.solid : ""}`}>
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

        {status === "member" ? (
          <NavbarAccount />
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
