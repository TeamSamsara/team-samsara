// File : /team-samsara/apps/web/src/features/auth/service/accountService.ts
// Version : 1.0.0
// Latest commit: task/web-auth-foundation
// Author : Gerrah
// Purpose : Every call to the account API, one function per step. Each returns an
// ApiOutcome and never throws, so screens decide what to show from the outcome alone.

import { AuthApiRoutes } from "../constants/authApiRoutes";
import {
  accountDeletionStatuses,
  authenticationStatuses,
  emailChangeStatuses,
  passwordStatuses,
  registrationStatuses,
  type AccountDeletionCodeResult,
  type AccountDeletionStatus,
  type AccountSummary,
  type EmailChangeCodeResult,
  type EmailChangeStatus,
  type LoginResult,
  type PasswordCodeResult,
  type PasswordResetTokenResult,
  type PasswordStatus,
  type RegistrationCodeResult,
  type RegistrationStatus,
  type AuthenticationStatus,
} from "../types/authTypes";
import {
  parseAccountSummary,
  parseCodeSent,
  parseNoContent,
  parseResetToken,
  parseStatus,
  postEmpty,
  postJson,
  sendAccountRequest,
  type ApiOutcome,
} from "./accountOutcome";

type Done<TStatus extends string> = { readonly status: TStatus };

const parseRegistrationCode = parseCodeSent(registrationStatuses);
const parseRegistrationStatus = parseStatus(registrationStatuses);
const parseLoginCode = parseCodeSent(authenticationStatuses);
const parseLoginStatus = parseStatus(authenticationStatuses);
const parsePasswordCode = parseCodeSent(passwordStatuses);
const parsePasswordStatus = parseStatus(passwordStatuses);
const parseEmailCode = parseCodeSent(emailChangeStatuses);
const parseEmailStatus = parseStatus(emailChangeStatuses);
const parseDeletionCode = parseCodeSent(accountDeletionStatuses);
const parseDeletionStatus = parseStatus(accountDeletionStatuses);

// ---- Who am I ----

export function getAccount(): Promise<ApiOutcome<AccountSummary>> {
  return sendAccountRequest(AuthApiRoutes.me, parseAccountSummary);
}

// ---- Registration ----

export function startRegistration(): Promise<
  ApiOutcome<RegistrationCodeResult>
> {
  return sendAccountRequest(
    AuthApiRoutes.register,
    parseRegistrationCode,
    postEmpty,
  );
}

export function resendRegistrationCode(): Promise<
  ApiOutcome<RegistrationCodeResult>
> {
  return sendAccountRequest(
    AuthApiRoutes.registerResend,
    parseRegistrationCode,
    postEmpty,
  );
}

export function confirmRegistration(
  code: string,
): Promise<ApiOutcome<Done<RegistrationStatus>>> {
  return sendAccountRequest(
    AuthApiRoutes.registerConfirm,
    parseRegistrationStatus,
    postJson({ code }),
  );
}

// ---- Login ----

export function checkLogin(): Promise<ApiOutcome<LoginResult>> {
  return sendAccountRequest(
    AuthApiRoutes.loginCheck,
    parseLoginCode,
    postEmpty,
  );
}

export function resendLoginCode(): Promise<ApiOutcome<LoginResult>> {
  return sendAccountRequest(
    AuthApiRoutes.loginResend,
    parseLoginCode,
    postEmpty,
  );
}

export function confirmLogin(
  code: string,
): Promise<ApiOutcome<Done<AuthenticationStatus>>> {
  return sendAccountRequest(
    AuthApiRoutes.loginConfirm,
    parseLoginStatus,
    postJson({ code }),
  );
}

// ---- Log out ----

export function logOut(): Promise<ApiOutcome<true>> {
  return sendAccountRequest(AuthApiRoutes.logout, parseNoContent, postEmpty);
}

export function logOutEverywhere(): Promise<ApiOutcome<true>> {
  return sendAccountRequest(AuthApiRoutes.logoutAll, parseNoContent, postEmpty);
}

// ---- Forgot password (signed out) ----

export function requestPasswordReset(
  email: string,
): Promise<ApiOutcome<PasswordCodeResult>> {
  return sendAccountRequest(
    AuthApiRoutes.passwordResetRequest,
    parsePasswordCode,
    postJson({ email }),
  );
}

export function verifyPasswordResetCode(
  email: string,
  code: string,
): Promise<ApiOutcome<PasswordResetTokenResult>> {
  return sendAccountRequest(
    AuthApiRoutes.passwordResetVerify,
    parseResetToken,
    postJson({ email, code }),
  );
}

export function setNewPassword(
  resetToken: string,
  newPassword: string,
): Promise<ApiOutcome<Done<PasswordStatus>>> {
  return sendAccountRequest(
    AuthApiRoutes.passwordResetConfirm,
    parsePasswordStatus,
    postJson({ resetToken, newPassword }),
  );
}

// ---- Change password (signed in) ----

export function requestPasswordChangeCode(
  newPassword: string,
): Promise<ApiOutcome<PasswordCodeResult>> {
  return sendAccountRequest(
    AuthApiRoutes.passwordRequestCode,
    parsePasswordCode,
    postJson({ newPassword }),
  );
}

export function changePassword(
  newPassword: string,
  code: string,
): Promise<ApiOutcome<Done<PasswordStatus>>> {
  return sendAccountRequest(
    AuthApiRoutes.passwordChange,
    parsePasswordStatus,
    postJson({ newPassword, code }),
  );
}

// ---- Change email ----

export function requestEmailChange(
  newEmail: string,
): Promise<ApiOutcome<EmailChangeCodeResult>> {
  return sendAccountRequest(
    AuthApiRoutes.emailRequest,
    parseEmailCode,
    postJson({ newEmail }),
  );
}

export function confirmEmailChange(
  oldCode: string,
  newCode: string,
): Promise<ApiOutcome<Done<EmailChangeStatus>>> {
  return sendAccountRequest(
    AuthApiRoutes.emailConfirm,
    parseEmailStatus,
    postJson({ oldCode, newCode }),
  );
}

// ---- Delete account ----

export function requestAccountDeletionCode(): Promise<
  ApiOutcome<AccountDeletionCodeResult>
> {
  return sendAccountRequest(
    AuthApiRoutes.deleteRequestCode,
    parseDeletionCode,
    postEmpty,
  );
}

export function deleteAccount(
  code: string,
): Promise<ApiOutcome<Done<AccountDeletionStatus>>> {
  return sendAccountRequest(
    AuthApiRoutes.deleteAccount,
    parseDeletionStatus,
    postJson({ code }),
  );
}
