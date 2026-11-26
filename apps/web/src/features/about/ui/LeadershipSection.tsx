// File : /team-samsara/apps/web/src/features/about/ui/LeadershipSection.tsx
// Version : 2.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : The "Our leadership" carousel, built on the shared Carousel and its sliding track.
// A row of slanted portraits starts at the page margin and runs off the right edge; the arrows
// (or the arrow keys, or a click on a portrait) slide the row one portrait at a time and it
// loops with no gap. Until the list has someone in it, it shows empty slots so it keeps its shape.

import { Carousel } from "@/shared/components/Carousel/Carousel";
import { CarouselNext } from "@/shared/components/Carousel/CarouselNext";
import { CarouselPrevious } from "@/shared/components/Carousel/CarouselPrevious";
import { CarouselTrack } from "@/shared/components/Carousel/CarouselTrack";
import { CarouselTrackItem } from "@/shared/components/Carousel/CarouselTrackItem";
import {
  aboutLeaderPlaceholderCount,
  aboutLeaders,
  type AboutLeader,
} from "../data/aboutContent";
import { AboutPageText } from "../view/aboutPageContent";
import { AboutMedia } from "./AboutMedia";
import layout from "./aboutLayout.module.css";
import styles from "./LeadershipSection.module.css";

/** One portrait slot: a leader, or an empty slanted panel while there are none yet. */
type LeaderSlot = AboutLeader | { readonly id: string };

/** The portrait that just left stays in view at the left while it slides out. */
const portraitsBeforeActive = 1;

function buildSlots(): readonly LeaderSlot[] {
  if (aboutLeaders.length > 0) return aboutLeaders;
  return Array.from({ length: aboutLeaderPlaceholderCount }, (_, index) => ({
    id: `placeholder-${index}`,
  }));
}

const slots = buildSlots();

function isLeader(slot: LeaderSlot): slot is AboutLeader {
  return "name" in slot;
}

function Arrow({ direction }: { readonly direction: "previous" | "next" }) {
  const path =
    direction === "previous"
      ? "M20 12H4M10 6l-6 6 6 6"
      : "M4 12h16M14 6l6 6-6 6";

  return (
    <svg
      viewBox="0 0 24 24"
      width="100%"
      height="100%"
      fill="none"
      stroke="currentColor"
      strokeWidth="2.5"
      strokeLinecap="square"
      aria-hidden="true"
    >
      <path d={path} />
    </svg>
  );
}

export function LeadershipSection() {
  return (
    <section className={styles.section} aria-labelledby="about-leadership">
      <div className={layout.wrap}>
        <h2 id="about-leadership" className={layout.bigHeading}>
          {AboutPageText.leadership}
        </h2>
      </div>

      <Carousel
        label={AboutPageText.leadership}
        count={slots.length}
        loop
        className={styles.carousel}
      >
        <CarouselTrack align="start" className={styles.track}>
          {slots.map((slot, index) => (
            <CarouselTrackItem
              key={slot.id}
              index={index}
              leading={portraitsBeforeActive}
            >
              <AboutMedia
                media={isLeader(slot) ? slot.media : { alt: "" }}
                className={styles.portrait}
              />
              {isLeader(slot) ? (
                <>
                  <p className={styles.name}>{slot.name}</p>
                  <p className={styles.role}>{slot.role}</p>
                </>
              ) : null}
            </CarouselTrackItem>
          ))}
        </CarouselTrack>

        <CarouselPrevious
          label={AboutPageText.previousLeader}
          className={`${styles.arrow} ${styles.previous}`}
        >
          <Arrow direction="previous" />
        </CarouselPrevious>
        <CarouselNext
          label={AboutPageText.nextLeader}
          className={`${styles.arrow} ${styles.next}`}
        >
          <Arrow direction="next" />
        </CarouselNext>
      </Carousel>
    </section>
  );
}
