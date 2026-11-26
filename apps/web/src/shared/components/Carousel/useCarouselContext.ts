// File : /team-samsara/apps/web/src/shared/components/Carousel/useCarouselContext.ts
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : Reads CarouselContext for the carousel parts, and fails loudly if
// a part is used outside <Carousel>.

import { useContext } from "react";
import { DevInvariants } from "@/lib/devInvariants";
import { CarouselContext } from "./CarouselContext";
import type { CarouselState } from "./useCarousel";

export function useCarouselContext(): CarouselState {
  const context = useContext(CarouselContext);
  if (!context) {
    throw new Error(DevInvariants.carouselPartOutsideCarousel);
  }
  return context;
}
