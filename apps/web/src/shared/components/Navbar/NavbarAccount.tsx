// File : /team-samsara/apps/web/src/shared/components/Navbar/NavbarAccount.tsx
// Version : 1.2.0
// Latest commit: task/web-account-security
// Author : Gerrah
// Purpose : The avatar menu in the navbar: who is signed in, edit profile, account security and log out.

import { Link } from "react-router-dom";
import { useAuth } from "@/app/useAuth";
import { useMyProfile } from "@/features/account/hooks/useMyProfile";
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

  const displayName = profile?.displayName ?? "";
  const initial = displayName.charAt(0).toUpperCase();

  return (
    <Dropdown>
      <DropdownTrigger
        popup="menu"
        className={styles.avatarButton}
        aria-label={NavbarText.accountMenu}
      >
        {profile?.profilePictureAssetId ? (
          <img
            className={styles.avatarImage}
            src={assetUrl(profile.profilePictureAssetId)}
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
