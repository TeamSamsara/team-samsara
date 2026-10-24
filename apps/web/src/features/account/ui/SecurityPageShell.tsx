// File : /team-samsara/apps/web/src/features/account/ui/SecurityPageShell.tsx
// Version : 1.0.0
// Latest commit: task/web-account-security
// Author : Gerrah
// Purpose : The frame shared by the account security pages (change password, change
// email, delete account): the way back to the security overview, the page title and
// one card holding the current step.

import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import { routePaths } from "@/shared/constants/routePaths";
import styles from "./SecurityPageShell.module.css";
import { SecurityPageShellText } from "./securityPageShellContent";

interface SecurityPageShellProps {
  readonly title: string;
  /** The step being shown; it brings its own heading and buttons. */
  readonly children: ReactNode;
  /** Colors the card in the error color, for deleting the account. */
  readonly danger?: boolean;
}

export function SecurityPageShell({
  title,
  children,
  danger = false,
}: SecurityPageShellProps) {
  return (
    <main className={styles.page}>
      <Link to={routePaths.accountSecurity} className={styles.back}>
        {SecurityPageShellText.back}
      </Link>
      <h1 className={styles.title}>{title}</h1>
      <section className={`${styles.card} ${danger ? styles.danger : ""}`}>
        {children}
      </section>
    </main>
  );
}
