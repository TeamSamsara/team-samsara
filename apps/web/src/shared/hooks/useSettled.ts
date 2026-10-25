// File : /team-samsara/apps/web/src/shared/hooks/useSettled.ts
// Version : 1.0.0
// Latest commit: task/web-cleanup-and-glow
// Author : Gerrah
// Purpose : Returns false when a component first appears and true once the
// given number of milliseconds has passed. Used to keep a fade from playing
// while the page is still settling, for example while the browser restores
// the scroll position after a refresh.

import { useEffect, useState } from "react";

export function useSettled(delayMs: number): boolean {
  const [isSettled, setIsSettled] = useState(false);

  useEffect(() => {
    const timer = window.setTimeout(() => setIsSettled(true), delayMs);
    return () => window.clearTimeout(timer);
  }, [delayMs]);

  return isSettled;
}
