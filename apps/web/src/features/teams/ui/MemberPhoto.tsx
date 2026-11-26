// File : /team-samsara/apps/web/src/features/teams/ui/MemberPhoto.tsx
// Version : 1.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : The cut-out photo of a team member. Until a photo is uploaded it shows the first
// letter of their handle on a dark panel, so a card never looks broken.

import { assetUrl } from "@/lib/assetUrl";
import type { TeamMember } from "../types/teamTypes";
import styles from "./MemberPhoto.module.css";

interface MemberPhotoProps {
  readonly member: TeamMember;
  readonly className?: string;
}

export function MemberPhoto({ member, className }: MemberPhotoProps) {
  const classNames = [styles.photo, className].filter(Boolean).join(" ");

  if (!member.photoAssetId) {
    return (
      <div className={`${classNames} ${styles.empty}`} aria-hidden="true">
        {member.handle.charAt(0).toUpperCase()}
      </div>
    );
  }

  return (
    <img
      className={classNames}
      src={assetUrl(member.photoAssetId)}
      alt={member.handle}
      loading="lazy"
    />
  );
}
