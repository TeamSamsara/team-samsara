// File : /team-samsara/apps/web/src/shared/components/Carousel/CarouselViewport.tsx
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The visible window of a carousel. Holds the slides stacked on top
// of each other and tells screen readers when to announce slide changes.

import type { ReactNode } from "react";
import { useCarouselContext } from "./useCarouselContext";
import styles from "./Carousel.module.css";

interface CarouselViewportProps {
  className?: string;
  children: ReactNode;
}

export function CarouselViewport({
  className,
  children,
}: CarouselViewportProps) {
  const { isAutoplaying } = useCarouselContext();

  return (
    <div
      className={
        className ? `${styles.viewport} ${className}` : styles.viewport
      }
      // Announcing every automatic change would be noise; manual changes are announced.
      aria-live={isAutoplaying ? "off" : "polite"}
    >
      {children}
    </div>
  );
}
