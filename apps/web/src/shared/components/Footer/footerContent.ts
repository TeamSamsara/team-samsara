// File : /team-samsara/apps/web/src/shared/components/Footer/footerContent.ts
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The Footer's columns and visible text, kept out of the component.
// Add a column here to add it to every page. Only pages that exist today are listed.

import { navbarLinks } from "../Navbar/navbarContent";

export type FooterLink = {
  label: string;
  to: string;
};

export type FooterSection = {
  heading: string;
  links: readonly FooterLink[];
};

export const footerSections: readonly FooterSection[] = [
  { heading: "Explore", links: navbarLinks },
];

export const footerCurrentYear = new Date().getFullYear();

export const FooterText = {
  navigation: "Footer",
  copyright: (year: number) => `© ${year} Team Samsara. All rights reserved.`,
} as const;
