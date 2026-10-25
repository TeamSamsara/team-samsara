// File : /team-samsara/apps/web/src/shared/components/Navbar/navbarContent.ts
// Version : 1.4.0
// Latest commit: task/web-cleanup-and-glow
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
  changeLanguage: "Change language",
  languageMenu: "Languages",
  signIn: "Sign in",
  logIn: "Log in",
  accountMenu: "Your account",
  signedInAs: "Signed in as",
  editProfile: "Edit profile",
  accountSecurity: "Account security",
  logOut: "Log out",
} as const;

// How far the page must scroll (in px) before a transparent navbar turns solid.
export const navbarSolidAfterScrollY = 10;

// How long (in ms) after the navbar appears before its background fade is switched on.
// It lets the browser restore the scroll position first, so the bar is not seen fading in.
export const navbarFadeStartsAfterMs = 400;
