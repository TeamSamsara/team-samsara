// File : /team-samsara/apps/web/src/shared/components/Carousel/useCarousel.ts
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The logic shared by every carousel: which slide is active, moving
// between slides, and the optional autoplay timer. It renders nothing, so each
// page decides how its carousel looks.

import { useCallback, useEffect, useState } from "react";

export interface UseCarouselOptions {
  /** Number of slides. */
  count: number;
  /** Slide shown first. */
  initialIndex?: number;
  /** Whether next/previous wrap around at the ends. */
  loop?: boolean;
  /** Milliseconds between automatic slide changes. Leave out for manual only. */
  autoplayMs?: number;
}

export interface CarouselState {
  index: number;
  count: number;
  canGoNext: boolean;
  canGoPrevious: boolean;
  /** True while a timer is running (autoplay on, not paused, not at the end). */
  isAutoplaying: boolean;
  /** Changes every time the autoplay countdown restarts, so indicators can restart their animation. */
  countdownKey: string;
  goTo: (index: number) => void;
  goNext: () => void;
  goPrevious: () => void;
  pause: () => void;
  resume: () => void;
}

const reducedMotionQuery = "(prefers-reduced-motion: reduce)";

function clamp(value: number, min: number, max: number): number {
  return Math.min(Math.max(value, min), max);
}

export function useCarousel({
  count,
  initialIndex = 0,
  loop = false,
  autoplayMs,
}: UseCarouselOptions): CarouselState {
  const lastIndex = Math.max(count - 1, 0);
  const [rawIndex, setRawIndex] = useState(initialIndex);
  const [isPaused, setIsPaused] = useState(false);
  const [resumeCount, setResumeCount] = useState(0);

  // The slide list can shrink after rendering (for example when the CMS returns fewer slides).
  const index = clamp(rawIndex, 0, lastIndex);

  const canGoNext = loop ? count > 1 : index < lastIndex;
  const canGoPrevious = loop ? count > 1 : index > 0;

  const goTo = useCallback(
    (target: number) => setRawIndex(clamp(target, 0, lastIndex)),
    [lastIndex],
  );

  const goNext = useCallback(() => {
    if (!canGoNext) return;
    setRawIndex(index >= lastIndex ? 0 : index + 1);
  }, [canGoNext, index, lastIndex]);

  const goPrevious = useCallback(() => {
    if (!canGoPrevious) return;
    setRawIndex(index <= 0 ? lastIndex : index - 1);
  }, [canGoPrevious, index, lastIndex]);

  const pause = useCallback(() => setIsPaused(true), []);

  const resume = useCallback(() => {
    setIsPaused(false);
    setResumeCount((value) => value + 1);
  }, []);

  const prefersReducedMotion =
    typeof window !== "undefined" &&
    window.matchMedia(reducedMotionQuery).matches;

  const isAutoplaying =
    autoplayMs !== undefined &&
    autoplayMs > 0 &&
    canGoNext &&
    !isPaused &&
    !prefersReducedMotion;

  useEffect(() => {
    if (!isAutoplaying) return;
    const timer = window.setTimeout(goNext, autoplayMs);
    return () => window.clearTimeout(timer);
  }, [isAutoplaying, goNext, autoplayMs]);

  return {
    index,
    count,
    canGoNext,
    canGoPrevious,
    isAutoplaying,
    countdownKey: `${index}-${resumeCount}`,
    goTo,
    goNext,
    goPrevious,
    pause,
    resume,
  };
}
