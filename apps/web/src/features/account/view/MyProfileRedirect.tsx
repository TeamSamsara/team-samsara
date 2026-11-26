// File : /team-samsara/apps/web/src/features/account/view/MyProfileRedirect.tsx
// Version : 1.0.0
// Latest commit: task/web-member-profile
// Author : Gerrah
// Purpose : The old /account/profile address. It sends the member to their own
// profile page, where they now edit it. If the profile cannot be loaded it says
// so and offers to try again; a member with no handle yet goes home.

import { Navigate } from "react-router-dom";
import { Button } from "@/shared/components/Button/Button";
import { memberPath, routePaths } from "@/shared/constants/routePaths";
import { useMyProfile } from "../hooks/useMyProfile";
import styles from "./ProfileSettingsPage.module.css";
import { ProfileSettingsText } from "./profileSettingsContent";

export function MyProfileRedirect() {
  const { data: profile, isPending, refetch } = useMyProfile();

  if (profile) {
    return (
      <Navigate
        to={profile.handle ? memberPath(profile.handle) : routePaths.home}
        replace
      />
    );
  }

  if (isPending) {
    return null;
  }

  return (
    <div className={styles.status}>
      <p className={styles.statusText} role="alert">
        {ProfileSettingsText.loadFailed}
      </p>
      <Button onClick={() => void refetch()}>
        {ProfileSettingsText.retry}
      </Button>
    </div>
  );
}
