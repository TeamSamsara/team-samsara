// File : /team-samsara/apps/web/src/shared/components/Button/LinkButton.tsx
// Version : 1.0.0
// Latest commit: task/web-error-pages
// Author : Gerrah
// Purpose : A router link that looks like the filled button.

import { Link, type LinkProps } from "react-router-dom";
import styles from "./Button.module.css";

export function LinkButton({ className, ...rest }: LinkProps) {
  const classNames = [styles.button, className].filter(Boolean).join(" ");

  return <Link {...rest} className={classNames} />;
}
