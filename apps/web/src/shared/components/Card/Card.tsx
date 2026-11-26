// File : /team-samsara/apps/web/src/shared/components/Card/Card.tsx
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The box every card on the site is built from: rounded, clipped,
// and the anchor that its media, content and link are positioned against. It
// shows nothing of its own, so a page decides what a card contains, how big it
// is and how it looks.

import type { ElementType, HTMLAttributes } from "react";
import styles from "./Card.module.css";

interface CardProps extends HTMLAttributes<HTMLElement> {
  /** Element to render, for example "li" inside a list or "article" for a post. */
  as?: "div" | "article" | "li" | "section";
}

export function Card({
  as = "div",
  className,
  children,
  ...elementProps
}: CardProps) {
  const Element: ElementType = as;

  return (
    <Element
      {...elementProps}
      className={className ? `${styles.card} ${className}` : styles.card}
    >
      {children}
    </Element>
  );
}
