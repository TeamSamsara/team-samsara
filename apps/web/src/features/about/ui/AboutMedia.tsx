// File : /team-samsara/apps/web/src/features/about/ui/AboutMedia.tsx
// Version : 1.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : One picture of the About page. With an asset id it shows the stored picture;
// without one it shows a dark panel of the same size, so the page keeps its shape until
// the picture is uploaded.

import { assetUrl } from "@/lib/assetUrl";
import type { AboutMedia as AboutMediaData } from "../data/aboutContent";
import styles from "./AboutMedia.module.css";

interface AboutMediaProps {
  readonly media: AboutMediaData;
  readonly className?: string;
}

export function AboutMedia({ media, className }: AboutMediaProps) {
  const classNames = [styles.media, className].filter(Boolean).join(" ");

  if (!media.assetId) {
    return <div className={classNames} aria-hidden="true" />;
  }

  return (
    <img
      className={classNames}
      src={assetUrl(media.assetId)}
      alt={media.alt}
      loading="lazy"
    />
  );
}
