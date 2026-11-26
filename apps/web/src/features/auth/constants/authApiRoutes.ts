// File : /team-samsara/apps/web/src/features/auth/constants/authApiRoutes.ts
// Version : 1.0.0
// Latest commit: task/web-auth-foundation
// Author : Gerrah
// Purpose : Backend API routes called by the account flows: sign-up, sign-in, recovery,
// and the password, email and deletion steps of account settings.

export const AuthApiRoutes = {
  me: "/account/me",

  register: "/account/register",
  registerResend: "/account/register/resend",
  registerConfirm: "/account/register/confirm",

  loginCheck: "/account/login/check",
  loginConfirm: "/account/login/confirm",
  loginResend: "/account/login/resend",

  logout: "/account/logout",
  logoutAll: "/account/logout/all",

  passwordResetRequest: "/account/password/reset/request",
  passwordResetVerify: "/account/password/reset/verify",
  passwordResetConfirm: "/account/password/reset/confirm",

  passwordRequestCode: "/account/password/request-code",
  passwordChange: "/account/password/change",

  emailRequest: "/account/email/request",
  emailConfirm: "/account/email/confirm",

  deleteRequestCode: "/account/delete/request-code",
  deleteAccount: "/account/delete",
} as const;
