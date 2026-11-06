// File : /team-samsara/apps/web/src/features/account/view/MemberProfileView.tsx
// Version : 1.0.0
// Latest commit: task/web-member-profile
// Author : Gerrah
// Purpose : A member's profile as it is read: banner, overlapping picture, name,
// handle, bio and the socials block. It has the same layout as the editor, so
// switching between the two does not move anything. The owner also gets the
// Edit profile button.

import { assetUrl } from "@/lib/assetUrl";
import type { PublicProfile } from "../types/profileTypes";
import styles from "./MemberProfileView.module.css";
import { MemberProfileText } from "./memberProfileContent";

interface MemberProfileViewProps {
  profile: PublicProfile;
  canEdit: boolean;
  onEdit: () => void;
}

export function MemberProfileView({
  profile,
  canEdit,
  onEdit,
}: MemberProfileViewProps) {
  const pictureUrl = profile.profilePictureAssetId
    ? assetUrl(profile.profilePictureAssetId)
    : null;
  const bannerUrl = profile.bannerAssetId
    ? assetUrl(profile.bannerAssetId)
    : null;

  return (
    <div className={styles.page}>
      <div className={styles.banner}>
        {bannerUrl ? (
          <img className={styles.image} src={bannerUrl} alt="" />
        ) : null}
      </div>

      <div className={styles.identity}>
        <div className={styles.avatar}>
          <div className={styles.avatarFrame}>
            {pictureUrl ? (
              <img
                className={styles.image}
                src={pictureUrl}
                alt={MemberProfileText.pictureAlt(profile.displayName)}
              />
            ) : (
              <span className={styles.initial} aria-hidden="true">
                {profile.displayName.charAt(0)}
              </span>
            )}
          </div>
        </div>

        {canEdit ? (
          <div className={styles.actions}>
            <button type="button" className={styles.edit} onClick={onEdit}>
              {MemberProfileText.editProfile}
            </button>
          </div>
        ) : null}

        <div className={styles.details}>
          <h1 className={styles.name}>{profile.displayName}</h1>
          <p className={styles.handle}>
            {MemberProfileText.handlePrefix}
            {profile.handle}
          </p>

          {profile.bio ? (
            <p className={styles.bio}>{profile.bio}</p>
          ) : canEdit ? (
            <p className={styles.bioEmpty}>{MemberProfileText.emptyBio}</p>
          ) : null}

          <section className={styles.socials}>
            <h2 className={styles.socialsTitle}>
              {MemberProfileText.socialsTitle}
            </h2>
            <p className={styles.socialsSoon}>
              {MemberProfileText.socialsSoon}
            </p>
          </section>
        </div>
      </div>
    </div>
  );
}
