// File : /team-samsara/apps/web/src/shared/components/Carousel/CarouselContext.ts
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The context that lets the carousel parts (slides, arrows,
// indicators) read and control the state owned by <Carousel>. Context only,
// like AuthContext.ts; the hook that reads it is useCarouselContext.

import { createContext } from "react";
import type { CarouselState } from "./useCarousel";

export const CarouselContext = createContext<CarouselState | undefined>(
  undefined,
);
