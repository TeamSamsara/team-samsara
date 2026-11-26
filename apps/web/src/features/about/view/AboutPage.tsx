// File : /team-samsara/apps/web/src/features/about/view/AboutPage.tsx
// Version : 2.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : The public About page: who Team Samsara is. It runs from where the team started
// (hero and story) to what it stands for (mission and the energy behind it), what it does
// (company and pillars), how it works (the brand), who leads it and an invitation to join.
// The words and pictures come from data/aboutContent.ts.

import { aboutCompany, aboutEnergy, aboutMission } from "../data/aboutContent";
import { AboutHero } from "../ui/AboutHero";
import { BrandSection } from "../ui/BrandSection";
import { CallToActionSection } from "../ui/CallToActionSection";
import { LeadershipSection } from "../ui/LeadershipSection";
import { PillarsSection } from "../ui/PillarsSection";
import { SplitSection } from "../ui/SplitSection";
import { StorySection } from "../ui/StorySection";
import styles from "./AboutPage.module.css";

export function AboutPage() {
  return (
    <main className={styles.page}>
      <AboutHero />
      <StorySection />
      <SplitSection block={aboutMission} />
      <SplitSection block={aboutEnergy} reversed />
      <SplitSection block={aboutCompany} />
      <PillarsSection />
      <BrandSection />
      <LeadershipSection />
      <CallToActionSection />
    </main>
  );
}
