// File : /team-samsara/apps/web/src/shared/components/Card/CardMedia.tsx
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The picture behind a card's content. It fills the whole card and
// crops to fit, and an optional focal point keeps the important part of the
// photo in view, like the hero. An empty alt marks the picture as decoration.

import type { ImgHTMLAttributes } from "react";
import styles from "./Card.module.css";

interface CardMediaProps extends ImgHTMLAttributes<HTMLImageElement> {
  /** Describes the picture for screen readers. Use "" when it is only decoration. */
  alt: string;
  /** Which part of the picture stays visible when it is cropped, as a CSS object-position such as "50% 10%". */
  focus?: string;
}

export function CardMedia({
  focus,
  className,
  style,
  loading = "lazy",
  decoding = "async",
  ...imgProps
}: CardMediaProps) {
  return (
    <img
      {...imgProps}
      className={className ? `${styles.media} ${className}` : styles.media}
      style={focus ? { objectPosition: focus, ...style } : style}
      loading={loading}
      decoding={decoding}
    />
  );
}
