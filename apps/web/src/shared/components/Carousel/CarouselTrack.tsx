// File : /team-samsara/apps/web/src/shared/components/Carousel/CarouselTrack.tsx
// Version : 1.2.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : The sliding variant of a carousel window. Every item is placed relative to the active
// one (see CarouselTrackItem), so the active item sits in the middle with the items before and
// after it peeking in at the sides; with `loop` on the carousel, the row wraps around with no gap.
// Size each item with --carousel-item-width and the space between them with --carousel-item-gap
// on the className you pass. With align="start" the active item sits at the left edge of the
// window instead of the middle, and the items after it fill the rest of the row.

import type { ReactNode } from "react";
import styles from "./Carousel.module.css";

interface CarouselTrackProps {
  /** Where the active item sits: the middle of the window (default) or its left edge. */
  align?: "center" | "start";
  className?: string;
  children: ReactNode;
}

export function CarouselTrack({
  align = "center",
  className,
  children,
}: CarouselTrackProps) {
  return (
    <div
      className={
        className ? `${styles.trackWindow} ${className}` : styles.trackWindow
      }
    >
      <div className={styles.track} data-align={align}>
        {children}
      </div>
    </div>
  );
}
