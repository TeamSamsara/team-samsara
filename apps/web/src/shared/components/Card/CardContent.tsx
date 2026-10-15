// File : /team-samsara/apps/web/src/shared/components/Card/CardContent.tsx
// Version : 1.0.0
// Latest commit: feature/web-home-page
// Author : Gerrah
// Purpose : The free layer on top of a card's picture. It holds whatever the
// card shows (a title, a countdown, a list, a button) and lays nothing out
// itself, so each page positions its content with its own styles.

import type { HTMLAttributes } from "react";
import styles from "./Card.module.css";

export function CardContent({
  className,
  children,
  ...divProps
}: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      {...divProps}
      className={className ? `${styles.content} ${className}` : styles.content}
    >
      {children}
    </div>
  );
}
