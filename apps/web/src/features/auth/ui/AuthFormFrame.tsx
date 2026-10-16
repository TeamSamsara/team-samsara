// File : /team-samsara/apps/web/src/features/auth/ui/AuthFormFrame.tsx
// Version : 1.0.0
// Latest commit: task/web-auth-screens
// Author : Gerrah
// Purpose : The shared body of the auth forms: logo, title, the fields, a message about the
// whole form, the submit button and the links underneath. Each screen supplies the parts.

import type { FormEventHandler, ReactNode } from "react";
import styles from "./AuthFormFrame.module.css";
import { AuthLogo } from "./AuthLogo";

export interface AuthFormFrameProps {
  title: string;
  onSubmit: FormEventHandler<HTMLFormElement>;
  /** A message about the whole form (a step that failed), shown above the button. */
  error?: string | null;
  /** The submit button. */
  actions: ReactNode;
  /** Links under the form. */
  footer?: ReactNode;
  /** The fields. */
  children: ReactNode;
}

export function AuthFormFrame({
  title,
  onSubmit,
  error,
  actions,
  footer,
  children,
}: AuthFormFrameProps) {
  return (
    <div className={styles.frame}>
      <AuthLogo />
      <h1 className={styles.title}>{title}</h1>

      <form className={styles.form} onSubmit={onSubmit} noValidate>
        <div className={styles.fields}>{children}</div>

        {error ? (
          <p className={styles.error} role="alert">
            {error}
          </p>
        ) : null}

        {actions}
      </form>

      {footer ? <div className={styles.footer}>{footer}</div> : null}
    </div>
  );
}
