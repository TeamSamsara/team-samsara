// File : /team-samsara/apps/web/src/shared/components/Carousel/CarouselArrow.tsx
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The previous / next button of a carousel. It only does the
// behavior (go to the neighbouring slide, disable at the ends when the
// carousel does not loop); the page supplies the icon and the look.

import type { ReactNode } from "react";
import { useCarouselContext } from "./useCarouselContext";

interface CarouselArrowProps {
  direction: "previous" | "next";
  /** Name read by screen readers, for example "Next slide". */
  label: string;
  className?: string;
  /** The icon or text shown inside the button. */
  children?: ReactNode;
}

export function CarouselArrow({
  direction,
  label,
  className,
  children,
}: CarouselArrowProps) {
  const { goNext, goPrevious, canGoNext, canGoPrevious } = useCarouselContext();
  const isNext = direction === "next";

  return (
    <button
      type="button"
      className={className}
      aria-label={label}
      disabled={isNext ? !canGoNext : !canGoPrevious}
      onClick={isNext ? goNext : goPrevious}
    >
      {children}
    </button>
  );
}
