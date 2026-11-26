// File : /team-samsara/apps/web/src/shared/components/Carousel/Carousel.tsx
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The root of every carousel. Owns the state (through useCarousel),
// shares it with the carousel parts, pauses autoplay while the visitor hovers
// or focuses it, and handles the arrow keys. It draws nothing of its own, so
// each page assembles the parts and styles them.

import { useRef, type KeyboardEvent, type ReactNode } from "react";
import { CarouselContext } from "./CarouselContext";
import { useCarousel, type UseCarouselOptions } from "./useCarousel";

interface CarouselProps extends UseCarouselOptions {
  /** Name read by screen readers, for example "Featured stories". */
  label: string;
  /** Pause autoplay while the pointer is over the carousel or focus is inside it. */
  pauseOnHover?: boolean;
  className?: string;
  children: ReactNode;
}

const editableTags = new Set(["INPUT", "TEXTAREA", "SELECT"]);

function isEditable(target: EventTarget): boolean {
  return (
    target instanceof HTMLElement &&
    (target.isContentEditable || editableTags.has(target.tagName))
  );
}

export function Carousel({
  label,
  pauseOnHover = true,
  className,
  children,
  ...options
}: CarouselProps) {
  const carousel = useCarousel(options);
  const { pause, resume, goNext, goPrevious } = carousel;
  const isHovered = useRef(false);
  const hasFocus = useRef(false);

  function syncPause() {
    if (isHovered.current || hasFocus.current) {
      pause();
    } else {
      resume();
    }
  }

  function handleKeyDown(event: KeyboardEvent<HTMLElement>) {
    if (isEditable(event.target)) return;
    if (event.key === "ArrowRight") {
      event.preventDefault();
      goNext();
    } else if (event.key === "ArrowLeft") {
      event.preventDefault();
      goPrevious();
    }
  }

  return (
    <CarouselContext.Provider value={carousel}>
      <section
        className={className}
        aria-roledescription="carousel"
        aria-label={label}
        onKeyDown={handleKeyDown}
        onMouseEnter={
          pauseOnHover
            ? () => {
                isHovered.current = true;
                syncPause();
              }
            : undefined
        }
        onMouseLeave={
          pauseOnHover
            ? () => {
                isHovered.current = false;
                syncPause();
              }
            : undefined
        }
        onFocus={
          pauseOnHover
            ? () => {
                hasFocus.current = true;
                syncPause();
              }
            : undefined
        }
        onBlur={
          pauseOnHover
            ? (event) => {
                if (event.currentTarget.contains(event.relatedTarget)) return;
                hasFocus.current = false;
                syncPause();
              }
            : undefined
        }
      >
        {children}
      </section>
    </CarouselContext.Provider>
  );
}
