// File : /team-samsara/apps/web/src/shared/components/Navbar/navbarContent.ts
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The Navbar's link list, visible text and scroll threshold, kept
// out of the component so the labels can be translated later.

import { routePaths } from "@/shared/constants/routePaths";

export const navbarLinks = [
  { label: "Home", to: routePaths.home },
  { label: "Teams", to: routePaths.teams },
  { label: "About Us", to: routePaths.about },
  { label: "Contact", to: routePaths.contact },
] as const;

export const NavbarText = {
  mainNavigation: "Main Navigation",
  language: "ENG",
  changeLanguage: "Change language",
  signIn: "Sign in",
  logIn: "Log in",
  notifications: "Notifications",
  account: "Your Account",
} as const;

// How far the page must scroll (in px) before a transparent navbar turns solid.
export const navbarSolidAfterScrollY = 10;
