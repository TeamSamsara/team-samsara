// File : /team-samsara/apps/web/src/shared/components/Carousel/CarouselNext.tsx
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The "next slide" button of a carousel. A named shortcut for
// CarouselArrow, so pages read <CarouselPrevious> and <CarouselNext>.

import type { ReactNode } from "react";
import { CarouselArrow } from "./CarouselArrow";

interface CarouselNextProps {
  /** Name read by screen readers, for example "Next slide". */
  label: string;
  className?: string;
  /** The icon or text shown inside the button. */
  children?: ReactNode;
}

export function CarouselNext(props: CarouselNextProps) {
  return <CarouselArrow direction="next" {...props} />;
}
