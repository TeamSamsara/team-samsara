// File : /team-samsara/apps/web/src/features/auth/ui/AuthFormFrame.tsx
// Version : 1.2.0
// Latest commit: task/web-toast-messages
// Author : Gerrah
// Purpose : The shared body of the auth forms: logo, title, the fields, a message about the
// whole form, the submit button and the links underneath. Each screen supplies the parts.

import type { FormEventHandler, ReactNode } from "react";
import { InlineMessage } from "@/shared/components/InlineMessage/InlineMessage";
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

        <InlineMessage message={error} withIcon className={styles.errorText} />

        {actions}
      </form>

      {footer ? <div className={styles.footer}>{footer}</div> : null}
    </div>
  );
}
