// File : /team-samsara/apps/web/src/lib/hrefKind.ts
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : Tells what kind of place an address leads to, so a link can be
// opened the right way: a page of this site (router, same tab), another
// website (new tab) or something else such as an email or phone link.

export type HrefKind = "internal" | "external" | "other";

const webProtocols = new Set(["http:", "https:"]);

export function getHrefKind(href: string, currentOrigin: string): HrefKind {
  let url: URL;
  try {
    url = new URL(href, currentOrigin);
  } catch {
    return "other";
  }
  if (!webProtocols.has(url.protocol)) return "other";
  return url.origin === currentOrigin ? "internal" : "external";
}
