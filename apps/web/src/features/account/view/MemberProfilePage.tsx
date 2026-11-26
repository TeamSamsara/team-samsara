// File : /team-samsara/apps/web/src/features/account/view/MemberProfilePage.tsx
// Version : 1.0.0
// Latest commit: task/web-member-profile
// Author : Gerrah
// Purpose : The page at /members/:handle, for signed-in members. It shows the
// profile of whoever owns the handle. On your own profile it reads from your
// own cached profile and offers Edit profile, which swaps the page for the
// editor in place. Saving a new handle moves the address to the new one.

import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Button } from "@/shared/components/Button/Button";
import { NotFoundPage } from "@/shared/components/StatusPage/NotFoundPage";
import { memberPath } from "@/shared/constants/routePaths";
import { useMemberProfile } from "../hooks/useMemberProfile";
import { useMyProfile } from "../hooks/useMyProfile";
import type { ProfileView, PublicProfile } from "../types/profileTypes";
import { normalizeHandle } from "../validation/profileValidation";
import { MemberProfileView } from "./MemberProfileView";
import styles from "./ProfileSettingsPage.module.css";
import { ProfileSettingsForm } from "./ProfileSettingsForm";
import { MemberProfileText } from "./memberProfileContent";

interface MemberProfileScreenProps {
  handle: string;
}

// What the page shows of your own profile, taken from the cache the editor saves into.
function ownProfile(view: ProfileView, handle: string): PublicProfile {
  return {
    displayName: view.displayName,
    handle,
    bio: view.bio,
    profilePictureAssetId: view.profilePictureAssetId,
    bannerAssetId: view.bannerAssetId,
  };
}

function MemberProfileScreen({ handle }: MemberProfileScreenProps) {
  const navigate = useNavigate();
  const [isEditing, setIsEditing] = useState(false);

  const mine = useMyProfile();
  const isOwn = mine.data?.handle != null && mine.data.handle === handle;
  const lookup = useMemberProfile(handle, !isOwn && !mine.isPending);

  function finishEditing(savedHandle: string) {
    setIsEditing(false);

    if (savedHandle !== handle) {
      void navigate(memberPath(savedHandle), { replace: true });
    }
  }

  if (isOwn && mine.data) {
    if (isEditing) {
      return (
        <ProfileSettingsForm
          profile={mine.data}
          onLeave={() => setIsEditing(false)}
          onSaved={finishEditing}
        />
      );
    }

    return (
      <MemberProfileView
        profile={ownProfile(mine.data, handle)}
        canEdit
        onEdit={() => setIsEditing(true)}
      />
    );
  }

  if (mine.isPending || lookup.isPending) {
    return null;
  }

  const outcome = lookup.data;

  if (outcome?.kind === "found") {
    return (
      <MemberProfileView
        profile={outcome.profile}
        canEdit={false}
        onEdit={() => undefined}
      />
    );
  }

  if (outcome?.kind === "notFound") {
    return <NotFoundPage />;
  }

  return (
    <div className={styles.status}>
      <p className={styles.statusText} role="alert">
        {outcome?.kind === "rateLimited"
          ? MemberProfileText.rateLimited
          : MemberProfileText.loadFailed}
      </p>
      <Button onClick={() => void lookup.refetch()}>
        {MemberProfileText.retry}
      </Button>
    </div>
  );
}

export function MemberProfilePage() {
  const { handle = "" } = useParams();
  const wanted = normalizeHandle(handle);

  // Keyed by the handle, so moving to another profile never keeps the editor open.
  return <MemberProfileScreen key={wanted} handle={wanted} />;
}
