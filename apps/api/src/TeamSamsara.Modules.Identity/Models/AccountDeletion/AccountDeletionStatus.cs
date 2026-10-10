// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/AccountDeletion/AccountDeletionStatus.cs
// Version : 1.0.0
// Latest commit: feat/account-deletion
// Author : Gerrah
// Purpose : The outcome of an account deletion step. Endpoints translate these into responses.

namespace TeamSamsara.Modules.Identity.Models;

public enum AccountDeletionStatus
{
    // The step succeeded (code sent, or account deleted)
    Success,

    // No active member account exists for this caller (missing, not a member, or already deleted)
    AccountNotFound,

    // A code was sent too recently; nothing was sent this time
    CooldownActive,

    // The submitted code was wrong
    InvalidCode,

    // The submitted code has expired
    CodeExpired,

    // Too many wrong guesses; a new code must be requested
    TooManyAttempts,

    // No code is pending for this account, or another request already used it to delete the account
    NoPendingCode
}
