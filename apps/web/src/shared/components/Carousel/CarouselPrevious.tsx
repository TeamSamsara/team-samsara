// File : /team-samsara/apps/web/src/shared/components/Carousel/CarouselPrevious.tsx
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The "previous slide" button of a carousel. A named shortcut for
// CarouselArrow, so pages read <CarouselPrevious> and <CarouselNext>.

import type { ReactNode } from "react";
import { CarouselArrow } from "./CarouselArrow";

interface CarouselPreviousProps {
  /** Name read by screen readers, for example "Previous slide". */
  label: string;
  className?: string;
  /** The icon or text shown inside the button. */
  children?: ReactNode;
}

export function CarouselPrevious(props: CarouselPreviousProps) {
  return <CarouselArrow direction="previous" {...props} />;
}
