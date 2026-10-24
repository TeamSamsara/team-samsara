// File : /team-samsara/apps/web/src/shared/components/Button/Button.tsx
// Version : 1.0.0
// Latest commit: task/web-auth-screens
// Author : Gerrah
// Purpose : The generic filled button. Handles the busy state (disabled and announced)
// so a form cannot be submitted twice; every other behavior comes from the caller.

import type { ButtonHTMLAttributes } from "react";
import styles from "./Button.module.css";

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  /** Shows the busy state: the button is disabled and announced as busy. */
  isLoading?: boolean;
  /** Stretches the button to the width of its container. */
  fullWidth?: boolean;
}

export function Button({
  type = "button",
  isLoading = false,
  fullWidth = false,
  disabled,
  className,
  children,
  ...rest
}: ButtonProps) {
  const classNames = [
    styles.button,
    fullWidth ? styles.fullWidth : "",
    className,
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <button
      {...rest}
      type={type}
      className={classNames}
      disabled={disabled || isLoading}
      aria-busy={isLoading || undefined}
    >
      {children}
    </button>
  );
}
