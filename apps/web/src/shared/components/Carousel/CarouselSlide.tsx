// File : /team-samsara/apps/web/src/shared/components/Carousel/CarouselSlide.tsx
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : One slide of a carousel. Only the active slide is visible and
// reachable by keyboard; the page styles the slide's content and can target
// the active state with [data-active="true"].

import type { ReactNode } from "react";
import { useCarouselContext } from "./useCarouselContext";
import styles from "./Carousel.module.css";

interface CarouselSlideProps {
  /** Position of this slide, starting at 0. */
  index: number;
  className?: string;
  children: ReactNode;
}

export function CarouselSlide({
  index,
  className,
  children,
}: CarouselSlideProps) {
  const { index: activeIndex, count } = useCarouselContext();
  const isActive = index === activeIndex;

  return (
    <div
      className={className ? `${styles.slide} ${className}` : styles.slide}
      role="group"
      aria-roledescription="slide"
      aria-label={`${index + 1} of ${count}`}
      data-active={isActive}
      inert={!isActive}
    >
      {children}
    </div>
  );
}
