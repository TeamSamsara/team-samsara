// File : /team-samsara/apps/web/src/shared/icons/Icon.tsx
// Version : 1.0.0
// Latest commit: task/web-icons
// Author : Gerrah
// Purpose : Draws one icon from the registry by name, as a decorative image.

import { iconRegistry, type IconName } from "./iconRegistry";
import styles from "./Icon.module.css";

interface IconComponentProps {
  name: IconName;
  className?: string;
  /** A thicker or thinner line for the interface icons. Brand logos ignore it. */
  strokeWidth?: number;
}

export function Icon({ name, className, strokeWidth }: IconComponentProps) {
  const Glyph = iconRegistry[name];
  const classNames = [styles.icon, className].filter(Boolean).join(" ");

  return <Glyph className={classNames} strokeWidth={strokeWidth} aria-hidden />;
}
