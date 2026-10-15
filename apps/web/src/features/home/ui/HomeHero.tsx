// File : /team-samsara/apps/web/src/features/home/ui/HomeHero.tsx
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The full-width hero at the top of the home page: a carousel of
// team photos with the title, subtitle and call to action over each one, and a
// row of progress bars at the bottom that also jump to a slide. It assembles
// the shared Carousel parts; the slides come from data/heroSlides.ts.

import { Link } from "react-router-dom";
import { assetUrl } from "@/lib/assetUrl";
import { Carousel } from "@/shared/components/Carousel/Carousel";
import { CarouselIndicators } from "@/shared/components/Carousel/CarouselIndicators";
import { CarouselSlide } from "@/shared/components/Carousel/CarouselSlide";
import { CarouselViewport } from "@/shared/components/Carousel/CarouselViewport";
import { heroSlides } from "../data/heroSlides";
import { HomeHeroText, homeHeroAutoplayMs } from "./homeHeroContent";
import styles from "./HomeHero.module.css";

export function HomeHero() {
  // A full-width hero is always under the pointer, so pausing on hover would
  // stop it for most visitors. Clicking a bar or using the arrow keys still
  // changes the slide and restarts the countdown.
  return (
    <Carousel
      label={HomeHeroText.carouselLabel}
      count={heroSlides.length}
      loop
      autoplayMs={homeHeroAutoplayMs}
      pauseOnHover={false}
      className={styles.hero}
    >
      <CarouselViewport className={styles.viewport}>
        {heroSlides.map((slide, index) => (
          <CarouselSlide key={slide.id} index={index} className={styles.slide}>
            <img
              className={styles.image}
              src={assetUrl(slide.assetId)}
              alt={slide.alt}
              style={slide.focus ? { objectPosition: slide.focus } : undefined}
              loading={index === 0 ? "eager" : "lazy"}
              fetchPriority={index === 0 ? "high" : "auto"}
            />
            <div className={styles.content}>
              <h1 className={styles.title}>
                <span className={styles.titleText}>{slide.title}</span>
              </h1>
              <div className={styles.subtitleRow}>
                <p className={styles.subtitle}>{slide.subtitle}</p>
                <Link to={slide.cta.to} className={styles.cta}>
                  {slide.cta.label}
                </Link>
              </div>
            </div>
          </CarouselSlide>
        ))}
      </CarouselViewport>

      <CarouselIndicators
        className={styles.indicators}
        indicatorClassName={styles.indicator}
        getLabel={(index) => HomeHeroText.goToSlide(index + 1)}
      >
        {({ isAutoplaying, countdownKey }) => (
          <span
            key={countdownKey}
            className={styles.indicatorFill}
            data-running={isAutoplaying}
            style={{ animationDuration: `${homeHeroAutoplayMs}ms` }}
          />
        )}
      </CarouselIndicators>
    </Carousel>
  );
}
