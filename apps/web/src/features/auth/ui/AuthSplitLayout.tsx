// File : /team-samsara/apps/web/src/features/auth/ui/AuthSplitLayout.tsx
// Version : 1.0.0
// Latest commit: task/web-auth-screens
// Author : Gerrah
// Purpose : The two-column frame of the sign-up, code and recovery screens: a dark panel
// with the headline on the left and the form centered on the right.

import type { ReactNode } from "react";
import { assetUrl } from "@/lib/assetUrl";
import { authArtAssetId } from "../data/authArt";
import styles from "./AuthSplitLayout.module.css";
import { AuthSplitLayoutText } from "./authSplitLayoutContent";

export function AuthSplitLayout({ children }: { children: ReactNode }) {
  return (
    <div className={styles.layout}>
      <aside className={styles.art}>
        {authArtAssetId ? (
          <>
            <img
              className={styles.photo}
              src={assetUrl(authArtAssetId)}
              alt=""
            />
            <div className={styles.shade} />
          </>
        ) : null}

        <p className={styles.headline}>
          {AuthSplitLayoutText.headlineLines.map((line) => (
            <span key={line} className={styles.line}>
              {line}
            </span>
          ))}
        </p>
        <p className={styles.body}>{AuthSplitLayoutText.body}</p>
      </aside>

      <main className={styles.content}>{children}</main>
    </div>
  );
}
