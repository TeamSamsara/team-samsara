// File : /team-samsara/apps/web/src/features/auth/types/authTypes.ts
// Version : 1.1.0
// Latest commit: task/web-account-security
// Author : Gerrah
// Purpose : The account API's request and response shapes, written once for the whole app.
// Each status list mirrors the backend enum, so a value the client does not know is detectable.

// ---- Who the caller is (GET /account/me) ----

export const accessLevels = ["guest", "member", "admin"] as const;
export type AccessLevel = (typeof accessLevels)[number];

export const authStates = [
  "anonymous",
  "registrationIncomplete",
  "verificationRequired",
  "member",
] as const;
export type AuthState = (typeof authStates)[number];

export interface AccountSummary {
  readonly userId: string;
  readonly accessLevel: AccessLevel;
  readonly authState: AuthState;
}

// ---- Outcomes of each flow ----

export const registrationStatuses = [
  "success",
  "alreadyRegistered",
  "accountNotFound",
  "cooldownActive",
  "tooManyAttempts",
  "invalidCode",
  "codeExpired",
  "noPendingCode",
] as const;
export type RegistrationStatus = (typeof registrationStatuses)[number];

export const authenticationStatuses = [
  "authenticated",
  "challengeRequired",
  "cooldownActive",
  "registrationIncomplete",
  "accountNotFound",
  "invalidCode",
  "codeExpired",
  "tooManyAttempts",
  "noPendingCode",
] as const;
export type AuthenticationStatus = (typeof authenticationStatuses)[number];

export const passwordStatuses = [
  "success",
  "accountNotFound",
  "invalidPassword",
  "cooldownActive",
  "invalidCode",
  "codeExpired",
  "tooManyAttempts",
  "noPendingCode",
  "invalidResetToken",
] as const;
export type PasswordStatus = (typeof passwordStatuses)[number];

export const emailChangeStatuses = [
  "success",
  "codeAccepted",
  "accountNotFound",
  "invalidEmail",
  "cooldownActive",
  "invalidCode",
  "tooManyAttempts",
  "noPendingRequest",
  "emailTaken",
  "sameAsCurrentEmail",
] as const;
export type EmailChangeStatus = (typeof emailChangeStatuses)[number];

export const accountDeletionStatuses = [
  "success",
  "accountNotFound",
  "cooldownActive",
  "invalidCode",
  "codeExpired",
  "tooManyAttempts",
  "noPendingCode",
] as const;
export type AccountDeletionStatus = (typeof accountDeletionStatuses)[number];

// ---- Response bodies ----

// A step that sent a code: how many characters it has and how long until another may be sent
export interface CodeSentResult<TStatus extends string> {
  readonly status: TStatus;
  readonly codeLength: number;
  readonly retryAfterSeconds: number;
}

export type RegistrationCodeResult = CodeSentResult<RegistrationStatus>;
export type LoginResult = CodeSentResult<AuthenticationStatus>;
export type PasswordCodeResult = CodeSentResult<PasswordStatus>;
export type EmailChangeCodeResult = CodeSentResult<EmailChangeStatus>;
export type AccountDeletionCodeResult = CodeSentResult<AccountDeletionStatus>;

export interface PasswordResetTokenResult {
  readonly status: PasswordStatus;
  readonly resetToken: string | null;
}

// ---- Request bodies ----

export interface ConfirmCodeRequest {
  readonly code: string;
}

export interface PasswordResetRequest {
  readonly email: string;
}

export interface VerifyResetCodeRequest {
  readonly email: string;
  readonly code: string;
}

export interface SetNewPasswordRequest {
  readonly resetToken: string;
  readonly newPassword: string;
}

export interface PasswordCodeRequest {
  readonly newPassword: string;
}

export interface ChangePasswordRequest {
  readonly newPassword: string;
  readonly code: string;
}

export interface ChangeEmailRequest {
  readonly newEmail: string;
}

export interface ConfirmEmailChangeRequest {
  readonly oldCode?: string;
  readonly newCode?: string;
}

export interface DeleteAccountRequest {
  readonly code: string;
}
