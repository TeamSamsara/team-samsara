// File : /team-samsara/apps/web/src/features/account/ui/OnboardingSlideshow.tsx
// Version : 1.0.0
// Latest commit: task/web-auth-onboarding-navbar
// Author : Gerrah
// Purpose : The changing photos on the left of the onboarding screens, with
// one dot per photo.

import { assetUrl } from "@/lib/assetUrl";
import { Carousel } from "@/shared/components/Carousel/Carousel";
import { CarouselIndicators } from "@/shared/components/Carousel/CarouselIndicators";
import { CarouselSlide } from "@/shared/components/Carousel/CarouselSlide";
import { CarouselViewport } from "@/shared/components/Carousel/CarouselViewport";
import {
  onboardingSlideAutoplayMs,
  onboardingSlides,
} from "../data/onboardingSlides";
import { OnboardingSlideshowText } from "./onboardingSlideshowContent";
import styles from "./OnboardingSlideshow.module.css";

export function OnboardingSlideshow() {
  return (
    <Carousel
      label={OnboardingSlideshowText.carouselLabel}
      count={onboardingSlides.length}
      loop
      autoplayMs={onboardingSlideAutoplayMs}
      pauseOnHover={false}
      className={styles.slideshow}
    >
      <CarouselViewport className={styles.viewport}>
        {onboardingSlides.map((slide, index) => (
          <CarouselSlide key={slide.id} index={index} className={styles.slide}>
            <img
              className={styles.image}
              src={assetUrl(slide.assetId)}
              alt={slide.alt}
              style={slide.focus ? { objectPosition: slide.focus } : undefined}
              loading={index === 0 ? "eager" : "lazy"}
            />
          </CarouselSlide>
        ))}
      </CarouselViewport>

      {/* Darkens the photos so the headline stays readable. */}
      <div className={styles.shade} />

      <CarouselIndicators
        className={styles.dots}
        indicatorClassName={styles.dot}
        getLabel={(index) => OnboardingSlideshowText.goToSlide(index + 1)}
      />
    </Carousel>
  );
}
