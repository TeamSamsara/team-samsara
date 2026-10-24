// File : /team-samsara/apps/web/src/features/account/view/ChangePasswordDoneStep.tsx
// Version : 1.1.0
// Latest commit: task/web-account-security
// Author : Gerrah
// Purpose : The last step of "change password": the password was changed and every
// session of the account was ended, so the signed-out message takes over.

import { SignedOutModal } from "../ui/SignedOutModal";
import { ChangePasswordText } from "./changePasswordContent";

export function ChangePasswordDoneStep() {
  return (
    <SignedOutModal
      title={ChangePasswordText.doneTitle}
      message={ChangePasswordText.doneDescription}
    />
  );
}
