// File : /team-samsara/apps/web/src/features/notifications/view/describeClient.ts
// Version : 1.0.0
// Latest commit: task/web-notifications
// Author : Gerrah
// Purpose : Reads a browser's user agent text into a browser name and an
// operating system name, for the sign-in notice. Either one can be missing.

export interface ClientDescription {
  readonly browser: string | null;
  readonly system: string | null;
}

interface ClientRule {
  readonly label: string;
  readonly pattern: RegExp;
}

// Order matters: Edge and Opera also say "Chrome", and Chrome also says "Safari".
const browserRules: readonly ClientRule[] = [
  { label: "Edge", pattern: /Edg(e|A|iOS)?\// },
  { label: "Opera", pattern: /OPR\/|Opera/ },
  { label: "Firefox", pattern: /Firefox\/|FxiOS\// },
  { label: "Chrome", pattern: /Chrome\/|CriOS\// },
  { label: "Safari", pattern: /Safari\// },
];

// Order matters: iPhones say "like Mac OS X", and Android also says "Linux".
const systemRules: readonly ClientRule[] = [
  { label: "Windows", pattern: /Windows/ },
  { label: "Android", pattern: /Android/ },
  { label: "iOS", pattern: /iPhone|iPad|iPod/ },
  { label: "macOS", pattern: /Mac OS X|Macintosh/ },
  { label: "ChromeOS", pattern: /CrOS/ },
  { label: "Linux", pattern: /Linux|X11/ },
];

function firstMatch(rules: readonly ClientRule[], text: string): string | null {
  return rules.find((rule) => rule.pattern.test(text))?.label ?? null;
}

export function describeClient(
  userAgent: string | undefined,
): ClientDescription {
  const text = userAgent ?? "";

  return {
    browser: firstMatch(browserRules, text),
    system: firstMatch(systemRules, text),
  };
}
