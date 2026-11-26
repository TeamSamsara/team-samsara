// File : /team-samsara/apps/web/src/features/auth/ui/AuthCenteredLayout.tsx
// Version : 1.0.0
// Latest commit: task/web-auth-screens
// Author : Gerrah
// Purpose : The single-column frame of the log in screen: the form centered on the page,
// with no navbar or footer, as in the design.

import type { ReactNode } from "react";
import styles from "./AuthCenteredLayout.module.css";

export function AuthCenteredLayout({ children }: { children: ReactNode }) {
  return <main className={styles.layout}>{children}</main>;
}
