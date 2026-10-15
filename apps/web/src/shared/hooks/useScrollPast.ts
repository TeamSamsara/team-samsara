// File : /team-samsara/apps/web/src/shared/hooks/useScrollPast.ts
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : Returns true once the page has scrolled further down than the
// given number of pixels.

import { useEffect, useState } from "react";

export function useScrollPast(threshold: number): boolean {
  const [isPast, setIsPast] = useState(() => window.scrollY > threshold);

  useEffect(() => {
    const onScroll = () => setIsPast(window.scrollY > threshold);
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => window.removeEventListener("scroll", onScroll);
  }, [threshold]);

  return isPast;
}
