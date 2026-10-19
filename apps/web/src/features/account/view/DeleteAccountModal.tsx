// File : /team-samsara/apps/web/src/features/account/view/DeleteAccountModal.tsx
// Version : 1.0.0
// Latest commit: task/web-account-security
// Author : Gerrah
// Purpose : The delete-account modal opened from the security overview. It runs three
// steps in one place: typing the handle to confirm, the code emailed to the account
// address, and the signed-out message. The handle check only saves a slip; the emailed
// code is what the API requires.

import { useState, type FormEvent } from "react";
import { useAuth } from "@/app/useAuth";
import type {
  CodeAttempt,
  ResendAttempt,
} from "@/features/auth/types/codeAttempt";
import {
  attemptFromProblem,
  resendAttemptFrom,
} from "@/features/auth/view/codeVerifyResults";
import { CodeVerifyMessages } from "@/features/auth/view/codeVerifyContent";
import { Modal } from "@/shared/components/Modal/Modal";
import { TextField } from "@/shared/components/Fields/TextField/TextField";
import { useMyProfile } from "../hooks/useMyProfile";
import {
  confirmDeletion,
  sendDeletionCode,
} from "../service/accountDeletionService";
import { SecurityCodeForm } from "../ui/SecurityCodeForm";
import styles from "../ui/SecurityStep.module.css";
import { SignedOutModal } from "../ui/SignedOutModal";
import { DeleteAccountText } from "./deleteAccountContent";

interface CodeSent {
  readonly codeLength: number;
  readonly retryAfterSeconds: number;
}

type Step =
  | { readonly kind: "handle" }
  | { readonly kind: "code"; readonly sent: CodeSent }
  | { readonly kind: "done" };

interface DeleteAccountModalProps {
  readonly onClose: () => void;
}

// A leading @ and capital letters do not matter when comparing handles.
function normalizeHandle(value: string): string {
  return value.trim().replace(/^@/, "").toLowerCase();
}

export function DeleteAccountModal({ onClose }: DeleteAccountModalProps) {
  const { user } = useAuth();
  const { data: profile } = useMyProfile();
  const [step, setStep] = useState<Step>({ kind: "handle" });
  const [typed, setTyped] = useState("");
  const [handleError, setHandleError] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [isSending, setIsSending] = useState(false);

  const handle = profile?.handle ?? null;

  async function continueToCode(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (isSending || !handle) {
      return;
    }

    if (normalizeHandle(typed) !== normalizeHandle(handle)) {
      setHandleError(DeleteAccountText.handleMismatch);
      return;
    }

    setHandleError(null);
    setFormError(null);
    setIsSending(true);

    const result = await sendDeletionCode();

    if (result.kind === "sent") {
      setStep({
        kind: "code",
        sent: {
          codeLength: result.codeLength,
          retryAfterSeconds: result.retryAfterSeconds,
        },
      });
      return;
    }

    setFormError(
      CodeVerifyMessages.failure(result.reason, result.retryAfterSeconds),
    );
    setIsSending(false);
  }

  async function confirm(code: string): Promise<CodeAttempt> {
    const result = await confirmDeletion(code);

    if (result.kind === "done") {
      setStep({ kind: "done" });
      return { kind: "accepted" };
    }

    return attemptFromProblem(result);
  }

  async function resend(): Promise<ResendAttempt> {
    return resendAttemptFrom(await sendDeletionCode());
  }

  if (step.kind === "done") {
    return (
      <SignedOutModal
        title={DeleteAccountText.doneTitle}
        message={DeleteAccountText.doneMessage}
      />
    );
  }

  if (step.kind === "code") {
    return (
      <Modal title={DeleteAccountText.codeTitle} dismissible={false}>
        <SecurityCodeForm
          description={DeleteAccountText.codeDescription(
            user?.email ?? "",
            step.sent.codeLength,
          )}
          codeLength={step.sent.codeLength}
          initialRetryAfterSeconds={step.sent.retryAfterSeconds}
          submitLabel={DeleteAccountText.codeSubmit}
          wrongCodeNote={DeleteAccountText.wrongCodeNote}
          danger
          onConfirm={confirm}
          onResend={resend}
          onCancel={onClose}
        />
      </Modal>
    );
  }

  return (
    <Modal
      title={DeleteAccountText.confirmTitle}
      dismissible={!isSending}
      onClose={onClose}
    >
      <form
        className={styles.step}
        onSubmit={(event) => void continueToCode(event)}
        noValidate
      >
        <p className={styles.description}>
          {DeleteAccountText.confirmDescription}
        </p>

        <div className={styles.fields}>
          <TextField
            label={DeleteAccountText.handleLabel}
            placeholder={handle ?? ""}
            name="confirm-handle"
            autoComplete="off"
            value={typed}
            error={handleError}
            onChange={(event) => {
              setTyped(event.target.value);
              setHandleError(null);
            }}
          />
        </div>

        {formError ? (
          <p className={`${styles.note} ${styles.noteError}`} role="alert">
            {formError}
          </p>
        ) : null}

        <div className={styles.actions}>
          <button
            type="submit"
            className={styles.primaryDanger}
            disabled={isSending || !handle || typed.trim().length === 0}
            aria-busy={isSending || undefined}
          >
            {DeleteAccountText.continue}
          </button>
          <button
            type="button"
            className={styles.outline}
            disabled={isSending}
            onClick={onClose}
          >
            {DeleteAccountText.cancel}
          </button>
        </div>
      </form>
    </Modal>
  );
}
