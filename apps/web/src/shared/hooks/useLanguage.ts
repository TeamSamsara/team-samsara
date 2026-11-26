// File : /team-samsara/apps/web/src/shared/hooks/useLanguage.ts
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The language the visitor picked. It is remembered in the browser,
// shared by every component that reads it, and written to <html lang> so
// screen readers and browsers know the page language. Nothing is translated
// yet; this holds the choice for when it is.

import { useEffect, useSyncExternalStore } from "react";
import {
  defaultLanguage,
  languageStorageKey,
  supportedLanguages,
  type LanguageCode,
} from "@/shared/constants/languages";

const listeners = new Set<() => void>();

function isLanguageCode(value: string | null): value is LanguageCode {
  return supportedLanguages.some((language) => language.code === value);
}

function readStoredLanguage(): LanguageCode {
  try {
    const stored = window.localStorage.getItem(languageStorageKey);
    return isLanguageCode(stored) ? stored : defaultLanguage;
  } catch {
    return defaultLanguage;
  }
}

let current: LanguageCode = readStoredLanguage();

function subscribe(listener: () => void): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

function getSnapshot(): LanguageCode {
  return current;
}

function setLanguage(next: LanguageCode): void {
  current = next;
  try {
    window.localStorage.setItem(languageStorageKey, next);
  } catch {
    // Private browsing can refuse storage; the choice then lasts until reload.
  }
  listeners.forEach((listener) => listener());
}

export function useLanguage(): {
  language: LanguageCode;
  setLanguage: (next: LanguageCode) => void;
} {
  const language = useSyncExternalStore(
    subscribe,
    getSnapshot,
    () => defaultLanguage,
  );

  useEffect(() => {
    document.documentElement.lang = language;
  }, [language]);

  return { language, setLanguage };
}
