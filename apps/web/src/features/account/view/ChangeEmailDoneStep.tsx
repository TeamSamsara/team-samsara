// File : /team-samsara/apps/web/src/features/account/view/ChangeEmailDoneStep.tsx
// Version : 1.1.0
// Latest commit: task/web-account-security
// Author : Gerrah
// Purpose : The last step of "change email": the address was changed and every session of
// the account was ended, so the signed-out message takes over and names the new address.

import { SignedOutModal } from "../ui/SignedOutModal";
import { ChangeEmailText } from "./changeEmailContent";

interface ChangeEmailDoneStepProps {
  readonly newEmail: string;
}

// Keeps the first character and the domain: "jane@example.com" becomes "j***@example.com".
function maskEmail(email: string): string {
  const at = email.lastIndexOf("@");

  if (at < 1) {
    return email;
  }

  return `${email.charAt(0)}***${email.slice(at)}`;
}

export function ChangeEmailDoneStep({ newEmail }: ChangeEmailDoneStepProps) {
  return (
    <SignedOutModal
      title={ChangeEmailText.doneHeading}
      message={`${ChangeEmailText.doneDescription(maskEmail(newEmail))} ${ChangeEmailText.doneNotice}`}
    />
  );
}
