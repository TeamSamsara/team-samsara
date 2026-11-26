// File : /team-samsara/apps/web/src/features/teams/ui/MemberModal.tsx
// Version : 1.0.0
// Latest commit: feat/web-contact-about-teams
// Author : Gerrah
// Purpose : The card of one team member, opened over the roster: their handle as the title, a
// close button, then the photo with the role, handle, real name and social links. Built on the
// shared Modal, so focus stays inside and Esc or a click outside closes it.

import { Modal } from "@/shared/components/Modal/Modal";
import { Icon } from "@/shared/icons/Icon";
import type { TeamMember } from "../types/teamTypes";
import { TeamRoleLabels, TeamsText } from "../view/teamsContent";
import { MemberPhoto } from "./MemberPhoto";
import { MemberSocials } from "./MemberSocials";
import styles from "./MemberModal.module.css";

interface MemberModalProps {
  readonly member: TeamMember;
  readonly onClose: () => void;
}

export function MemberModal({ member, onClose }: MemberModalProps) {
  return (
    <Modal title={member.handle} onClose={onClose} className={styles.dialog}>
      <button
        type="button"
        className={styles.close}
        aria-label={TeamsText.closeCard}
        onClick={onClose}
      >
        <Icon name="close" className={styles.closeIcon} strokeWidth={3} />
      </button>

      <div className={styles.body}>
        <MemberPhoto member={member} className={styles.photo} />
        <div className={styles.details}>
          <p className={styles.role}>{TeamRoleLabels[member.role]}</p>
          <p className={styles.handle}>{member.handle}</p>
          {member.realName ? (
            <p className={styles.realName}>{member.realName}</p>
          ) : null}
          <MemberSocials handle={member.handle} socials={member.socials} />
        </div>
      </div>
    </Modal>
  );
}
