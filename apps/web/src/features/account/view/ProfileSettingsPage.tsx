// File : /team-samsara/apps/web/src/features/account/view/ProfileSettingsPage.tsx
// Version : 1.0.0
// Latest commit: task/web-account-settings
// Author : Gerrah
// Purpose : The edit-profile page at /account/profile. It loads the member's own
// profile and hands it to the form; if the profile cannot be loaded it says so
// and offers to try again.

import { Button } from "@/shared/components/Button/Button";
import { useMyProfile } from "../hooks/useMyProfile";
import styles from "./ProfileSettingsPage.module.css";
import { ProfileSettingsForm } from "./ProfileSettingsForm";
import { ProfileSettingsText } from "./profileSettingsContent";

export function ProfileSettingsPage() {
  const { data: profile, isPending, refetch } = useMyProfile();

  if (profile) {
    return <ProfileSettingsForm profile={profile} />;
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
