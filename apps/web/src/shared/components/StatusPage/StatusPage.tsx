// File : /team-samsara/apps/web/src/shared/components/StatusPage/StatusPage.tsx
// Version : 1.0.0
// Latest commit: task/web-error-pages
// Author : Gerrah
// Purpose : Layout behind the 404, 403 and 500 pages.

import type { ReactNode } from "react";
import styles from "./StatusPage.module.css";

interface StatusPageProps {
  code: string;
  title: string;
  body: string;
  actions: ReactNode;
}

export function StatusPage({ code, title, body, actions }: StatusPageProps) {
  return (
    <section className={styles.status}>
      <p className={styles.code}>{code}</p>
      <h1 className={styles.title}>{title}</h1>
      <p className={styles.body}>{body}</p>
      <div className={styles.actions}>{actions}</div>
    </section>
  );
}
