// File : /team-samsara/apps/web/src/shared/components/Navbar/NavbarAccount.tsx
// Version : 1.0.0
// Latest commit: task/web-auth-onboarding-navbar
// Author : Gerrah
// Purpose : The account menu in the navbar: the member's avatar (their picture,
// or the first letter of their name) that opens a list with who is signed in,
// a link to edit the profile and a log out button. It assembles the shared
// Dropdown parts; the profile comes from useMyProfile.

import { Link } from "react-router-dom";
import { useAuth } from "@/app/useAuth";
import { useMyProfile } from "@/features/account/hooks/useMyProfile";
import { assetUrl } from "@/lib/assetUrl";
import { Dropdown } from "@/shared/components/Dropdown/Dropdown";
import { DropdownItem } from "@/shared/components/Dropdown/DropdownItem";
import { DropdownPanel } from "@/shared/components/Dropdown/DropdownPanel";
import { DropdownTrigger } from "@/shared/components/Dropdown/DropdownTrigger";
import { routePaths } from "@/shared/constants/routePaths";
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
          {NavbarText.editProfile}
        </DropdownItem>
        <DropdownItem
          role="menuitem"
          className={styles.accountItem}
          onSelect={() => void signOut()}
        >
          {NavbarText.logOut}
        </DropdownItem>
      </DropdownPanel>
    </Dropdown>
  );
}
