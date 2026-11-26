// File : /team-samsara/apps/web/src/features/about/data/aboutContent.ts
// Version : 2.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : What the About page says and shows. This file stands in for the CMS until it is
// built: the CMS will later return the same shapes, so the page will not need to change.
// A picture is an asset id (from POST /assets, shown through assetUrl); a slot with no asset
// id shows a dark panel until the picture is uploaded.

export type AboutMedia = {
  /** Asset id of the picture or video poster; leave out until it is uploaded. */
  assetId?: string;
  /** Describes the picture for screen readers. */
  alt: string;
};

/** A headline, some paragraphs and a picture: the building block of most of the page. */
export type AboutBlock = {
  id: string;
  title: string;
  paragraphs: readonly string[];
  media: AboutMedia;
};

export type AboutMilestone = {
  id: string;
  /** The small label above the title, for example the year. */
  label: string;
  title: string;
  body: string;
  media: AboutMedia;
};

export type AboutPillar = {
  id: string;
  title: string;
  body: string;
  media: AboutMedia;
};

export type AboutLeader = {
  id: string;
  name: string;
  role: string;
  media: AboutMedia;
};

export const aboutHero = {
  tagline: "GROW. EVOLVE. RENEW.",
  intro:
    "Founded in 2025 in the DMV and built from the ground up, powered by fans and local passion.",
  media: { alt: "Team Samsara" } satisfies AboutMedia,
};

export const aboutStory = {
  /** The headline is split so the last words can be highlighted. */
  headline:
    "Founded in 2025 in the DMV to prove that a team built from the ground up, powered by fans and local passion,",
  highlight: "can reach the LCS.",
  milestones: [
    {
      id: "founded",
      label: "2025",
      title: "Founded in Newark, Virginia",
      body: "One goal: prove that a team built from the ground up can reach the LCS.",
      media: { alt: "The founding of Team Samsara" },
    },
    {
      id: "dvm-tour",
      label: "DVM TOUR",
      title: "DVM Tour champions",
      body: "Proof that our approach works, and something tangible for everyone who has backed us to rally around.",
      media: { alt: "Team Samsara at the DVM Tour" },
    },
    {
      id: "lcs",
      label: "NEXT",
      title: "The climb to the LCS",
      body: "The climb is steep, but with the foundation we have built, the original dream feels closer every day.",
      media: { alt: "Team Samsara looking ahead" },
    },
  ] satisfies readonly AboutMilestone[],
};

export const aboutMission: AboutBlock = {
  id: "mission",
  title: "AN INSTITUTION, NOT JUST A ROSTER",
  paragraphs: [
    "Our mission was to build something that goes beyond one individual or a single set of players: a team that becomes part of who people are.",
    "The kind of team you grow up cheering for, that brings strangers together on game day, and that means something to the whole region, not just its fans.",
    "Players and rosters will change, but what Team Samsara represents won’t, because it belongs to the people who support it.",
  ],
  media: { alt: "The Team Samsara community" },
};

export const aboutEnergy: AboutBlock = {
  id: "energy",
  title: "THE ENERGY BEHIND US",
  paragraphs: [
    "Our co-streamers, Mirosca and Graffuko, bring fans into the action, turning matches into shared moments.",
    "Our recent DVM Tour championship proved that our approach works. With each new supporter, our momentum grows.",
  ],
  media: { alt: "Team Samsara co-streamers" },
};

export const aboutCompany: AboutBlock = {
  id: "company",
  title: "COMPETITION AND CREATIVITY, HAND IN HAND",
  paragraphs: [
    "Founded by players, creators, and designers, Samsara was built to connect audience, brand, and creative projects under one identity.",
    "Our competitive esports programs drive visibility, while our agency work in apparel and technology serves brands across industries, from gaming and sports to fashion and beyond.",
  ],
  media: { alt: "The Team Samsara company" },
};

export const aboutPillars: readonly AboutPillar[] = [
  {
    id: "competitive",
    title: "COMPETITIVE",
    body: "We are building a League of Legends program from the ground up: scouting talent, developing players and taking each season as it comes, from amateur leagues to one day winning Worlds.",
    media: { alt: "Team Samsara competitive esports" },
  },
  {
    id: "entertainment",
    title: "ENTERTAINMENT",
    body: "How Team Samsara exists beyond competition: content around our people, collaborations with artists and creators, and visual work that makes the brand distinct.",
    media: { alt: "Team Samsara entertainment" },
  },
  {
    id: "apparel",
    title: "APPAREL",
    body: "Collections for Team Samsara, plus an agency for brands that want to create clothing. Design that works as team apparel and everyday wear.",
    media: { alt: "Team Samsara apparel" },
  },
  {
    id: "technology",
    title: "TECHNOLOGY & DEVELOPMENT",
    body: "We design and build apps, software and digital products, for Team Samsara and for the brands we work with.",
    media: { alt: "Team Samsara technology and development" },
  },
];

export const aboutBrandIntro = {
  title: "A NAME THAT MEANS TRANSFORMATION",
  body: "Team Samsara comes from the Hindu concept of transformation, the cycle that constantly moves forward. Growth, change, evolution. For us, that cycle is Grow, Evolve, Renew.",
};

export const aboutBrandValues: readonly AboutBlock[] = [
  {
    id: "grow",
    title: "PLAYERS IMPROVE TO BECOME THE BEST",
    paragraphs: [
      "Players improve in order to become the best. Creators push our brand forward. Every season, every drop and every project is a chance to get better.",
    ],
    media: { alt: "Grow" },
  },
  {
    id: "evolve",
    title: "WE TEST WHAT WORKS, THEN BUILD ON IT",
    paragraphs: [
      "We test what works, we build community and we launch drops. Different work, same approach: we build, we learn, we improve.",
    ],
    media: { alt: "Evolve" },
  },
  {
    id: "renew",
    title: "WE GO AGAIN, STRONGER EACH CYCLE",
    paragraphs: [
      "We go again. What we learn from our own brand, we apply to the brands we work with, so every cycle starts stronger than the last.",
    ],
    media: { alt: "Renew" },
  },
];

export const aboutCallToAction = {
  title: "BE PART OF WHAT COMES NEXT",
  body: "If you see yourself in our journey, or just want to see what happens when a team and its supporters push for something bigger, you’re invited.",
};

/** The leadership carousel keeps its shape with empty slots until this list has people in it. */
export const aboutLeaders: readonly AboutLeader[] = [];

// How many empty portrait slots the leadership carousel shows until the leaders are added.
export const aboutLeaderPlaceholderCount = 6;
