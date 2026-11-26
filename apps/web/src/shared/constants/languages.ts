// File : /team-samsara/apps/web/src/shared/constants/languages.ts
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The languages the site offers: the code stored and set on the page,
// the short label shown in the language dropdown, and the full name read by
// screen readers. Adding a language later means adding one line here.

export const supportedLanguages = [
  { code: "en", label: "ENG", name: "English" },
  { code: "fr", label: "FR", name: "Français" },
] as const;

export type LanguageCode = (typeof supportedLanguages)[number]["code"];

export const defaultLanguage: LanguageCode = "en";

export const languageStorageKey = "team-samsara.language";
