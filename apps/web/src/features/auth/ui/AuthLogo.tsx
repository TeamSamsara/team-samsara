// File : /team-samsara/apps/web/src/features/auth/ui/AuthLogo.tsx
// Version : 1.2.0
// Latest commit: task/web-auth-screens
// Author : Gerrah
// Purpose : The Samsara icon shown above the auth forms. It links back to the
// home page, since these screens have no navbar.

import { Link } from "react-router-dom";
import { assetUrl } from "@/lib/assetUrl";
import { brandAssetIds } from "@/shared/constants/branding";
import { routePaths } from "@/shared/constants/routePaths";
import styles from "./AuthLogo.module.css";
import { AuthLogoText } from "./authLogoContent";

export function AuthLogo() {
  return (
    <Link
      to={routePaths.home}
      className={styles.logo}
      aria-label={AuthLogoText.homeLabel}
    >
      <img className={styles.icon} src={assetUrl(brandAssetIds.icon)} alt="" />
    </Link>
  );
}
