// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/Passwords/PasswordStatus.cs
// Version : 1.1.0
// Latest commit: feat/password-reset-token-store
// Author : Gerrah
// Purpose : The outcome of a password step. Endpoints translate these into responses.

namespace TeamSamsara.Modules.Identity.Models;

public enum PasswordStatus
{
    // The step succeeded (code sent, or password changed)
    Success,

    // No member account exists for this caller (missing in Firebase, never registered, or
    // registration not completed)
    AccountNotFound,

    // The new password does not meet the password rules; nothing was sent or changed
    InvalidPassword,

    // A code was sent too recently; nothing was sent this time
    CooldownActive,

    // The submitted code was wrong
    InvalidCode,

    // The submitted code has expired
    CodeExpired,

    // Too many wrong guesses; a new code must be requested
    TooManyAttempts,

    // No code is pending for this account
    NoPendingCode,

    // The password reset token is unknown, expired or already used. These are deliberately not
    // told apart, so the answer reveals nothing about which tokens ever existed.
    InvalidResetToken
}
