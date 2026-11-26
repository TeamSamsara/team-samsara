// File : /team-samsara/apps/web/src/shared/components/Carousel/CarouselIndicators.tsx
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : One button per slide, to show where the visitor is and jump to a
// slide. It does the behavior and the accessibility; the page decides what each
// indicator contains (a bar, a dot, a label) and how it looks.

import type { ReactNode } from "react";
import { useCarouselContext } from "./useCarouselContext";

export interface CarouselIndicatorInfo {
  index: number;
  isActive: boolean;
  /** True only for the active indicator while the autoplay countdown runs. */
  isAutoplaying: boolean;
  /** Use as a React key on an animated element so its animation restarts with the countdown. */
  countdownKey: string;
}

interface CarouselIndicatorsProps {
  /** Name read by screen readers for each indicator, for example "Go to slide 2". */
  getLabel: (index: number) => string;
  /** Class of the row that holds the indicators. */
  className?: string;
  /** Class of each indicator button. */
  indicatorClassName?: string;
  /** Draws the inside of each indicator. */
  children?: (info: CarouselIndicatorInfo) => ReactNode;
}

export function CarouselIndicators({
  getLabel,
  className,
  indicatorClassName,
  children,
}: CarouselIndicatorsProps) {
  const {
    index: activeIndex,
    count,
    isAutoplaying,
    countdownKey,
    goTo,
  } = useCarouselContext();

  return (
    <div className={className}>
      {Array.from({ length: count }, (_, index) => {
        const isActive = index === activeIndex;
        const isCountingDown = isActive && isAutoplaying;
        return (
          <button
            key={index}
            type="button"
            className={indicatorClassName}
            aria-label={getLabel(index)}
            aria-current={isActive ? "true" : undefined}
            data-active={isActive}
            data-autoplaying={isCountingDown}
            onClick={() => goTo(index)}
          >
            {children?.({
              index,
              isActive,
              isAutoplaying: isCountingDown,
              countdownKey,
            })}
          </button>
        );
      })}
    </div>
  );
}
