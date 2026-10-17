// File : /team-samsara/apps/web/src/shared/components/Navbar/navbarContent.ts
// Version : 1.2.0
// Latest commit: task/web-auth-onboarding-navbar
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
  logOut: "Log out",
} as const;

// How far the page must scroll (in px) before a transparent navbar turns solid.
export const navbarSolidAfterScrollY = 10;
