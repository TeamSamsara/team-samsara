// File : /team-samsara/apps/web/src/shared/components/Carousel/CarouselTrackItem.tsx
// Version : 1.2.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : One item of a CarouselTrack. It works out its position relative to the active item
// (0 is the middle, -1 the one before, 1 the one after; in a looping carousel the shortest way
// around), and slides there. An item that wraps from one end to the other jumps without sliding
// across the screen. `leading` is how many items stay in view before the active one (the
// default keeps about half of them, for a centred row). Items other than the active one are visible but hidden from screen readers
// and keyboard, and clicking one brings it to the middle.

import { useState, type CSSProperties, type ReactNode } from "react";
import { useCarouselContext } from "./useCarouselContext";
import styles from "./Carousel.module.css";

interface CarouselTrackItemProps {
  /** Position of this item, starting at 0. */
  index: number;
  /** How many items stay before the active one in a looping row. */
  leading?: number;
  className?: string;
  children: ReactNode;
}

/** Items need a neighbour on each side before wrapping around looks right. */
const minLoopCount = 3;

function offsetFromActive(
  index: number,
  activeIndex: number,
  count: number,
  loop: boolean,
  leading: number | undefined,
): number {
  const distance = index - activeIndex;
  if (!loop || count < minLoopCount) return distance;
  const wrapped = ((distance % count) + count) % count;
  if (leading === undefined)
    return wrapped > count / 2 ? wrapped - count : wrapped;
  return ((((distance + leading) % count) + count) % count) - leading;
}

export function CarouselTrackItem({
  index,
  leading,
  className,
  children,
}: CarouselTrackItemProps) {
  const { index: activeIndex, count, loop, goTo } = useCarouselContext();
  const isActive = index === activeIndex;
  const offset = offsetFromActive(index, activeIndex, count, loop, leading);

  // Remember where the item was, so a wrap from one end to the other can skip the slide.
  const [previousOffset, setPreviousOffset] = useState(offset);
  const [hasWrapped, setHasWrapped] = useState(false);
  if (offset !== previousOffset) {
    setPreviousOffset(offset);
    setHasWrapped(Math.abs(offset - previousOffset) > 1);
  }

  const style = {
    "--carousel-offset": offset,
    transition: hasWrapped ? "none" : undefined,
  } as CSSProperties;

  return (
    <div
      className={
        className ? `${styles.trackItem} ${className}` : styles.trackItem
      }
      style={style}
      role="group"
      aria-roledescription="slide"
      aria-label={`${index + 1} of ${count}`}
      data-active={isActive}
      inert={!isActive}
      onClick={isActive ? undefined : () => goTo(index)}
    >
      {children}
    </div>
  );
}
