// File : /team-samsara/apps/web/src/shared/components/Navbar/NavbarAccount.tsx
// Version : 1.3.0
// Latest commit: task/web-notifications
// Author : Gerrah
// Purpose : The avatar menu in the navbar: who is signed in, edit profile, account security and log out.
// Until the profile loads it draws the avatar from the last visit's note, so a refresh does not flash an empty one.

import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "@/app/useAuth";
import { useMyProfile } from "@/features/account/hooks/useMyProfile";
import { readAuthHint, saveAuthHint } from "@/lib/authHint";
import { assetUrl } from "@/lib/assetUrl";
import { Dropdown } from "@/shared/components/Dropdown/Dropdown";
import { DropdownItem } from "@/shared/components/Dropdown/DropdownItem";
import { DropdownPanel } from "@/shared/components/Dropdown/DropdownPanel";
import { DropdownTrigger } from "@/shared/components/Dropdown/DropdownTrigger";
import { routePaths } from "@/shared/constants/routePaths";
import { Icon } from "@/shared/icons/Icon";
import { NavbarText } from "./navbarContent";
import styles from "./Navbar.module.css";

export function NavbarAccount() {
  const { signOut } = useAuth();
  const { data: profile } = useMyProfile();
  const [hint] = useState(readAuthHint);

  const hasProfile = profile !== undefined;
  const displayName = profile?.displayName ?? "";
  const profileInitial = displayName.charAt(0).toUpperCase();
  const profilePictureAssetId = profile?.profilePictureAssetId ?? null;

  const initial = hasProfile ? profileInitial : (hint?.initial ?? "");
  const pictureAssetId = hasProfile
    ? profilePictureAssetId
    : (hint?.pictureAssetId ?? null);

  // The next refresh starts from what this visit showed.
  useEffect(() => {
    if (hasProfile) {
      saveAuthHint({
        initial: profileInitial,
        pictureAssetId: profilePictureAssetId,
      });
    }
  }, [hasProfile, profileInitial, profilePictureAssetId]);

  return (
    <Dropdown>
      <DropdownTrigger
        popup="menu"
        className={styles.avatarButton}
        aria-label={NavbarText.accountMenu}
      >
        {pictureAssetId ? (
          <img
            className={styles.avatarImage}
            src={assetUrl(pictureAssetId)}
            alt=""
          />
        ) : (
          initial
        )}
      </DropdownTrigger>
      <DropdownPanel
        role="menu"
        aria-label={NavbarText.accountMenu}
        placement="bottom-end"
        offset={14}
        className={styles.accountPanel}
      >
        {displayName && (
          <p className={styles.accountHeader}>
            {NavbarText.signedInAs}{" "}
            <span className={styles.accountName}>{displayName}</span>
          </p>
        )}
        <DropdownItem
          as={Link}
          to={routePaths.accountProfile}
          role="menuitem"
          className={styles.accountItem}
        >
          <Icon name="profileSettings" className={styles.accountIcon} />
          {NavbarText.editProfile}
        </DropdownItem>
        <DropdownItem
          as={Link}
          to={routePaths.accountSecurity}
          role="menuitem"
          className={styles.accountItem}
        >
          <Icon name="security" className={styles.accountIcon} />
          {NavbarText.accountSecurity}
        </DropdownItem>
        <DropdownItem
          role="menuitem"
          className={styles.accountItem}
          onSelect={() => void signOut()}
        >
          <Icon name="logOut" className={styles.accountIcon} />
          {NavbarText.logOut}
        </DropdownItem>
      </DropdownPanel>
    </Dropdown>
  );
}
