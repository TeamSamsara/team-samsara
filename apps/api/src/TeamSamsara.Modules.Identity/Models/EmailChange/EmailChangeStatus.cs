// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/EmailChange/EmailChangeStatus.cs
// Version : 1.0.0
// Latest commit: feat/email-change-service
// Author : Gerrah
// Purpose : The outcome of an email change step. Endpoints translate these into responses.

namespace TeamSamsara.Modules.Identity.Models;

public enum EmailChangeStatus
{
    // The step succeeded (codes requested, or email changed)
    Success,

    // No member account exists for this caller
    AccountNotFound,

    // The new address is not a valid email address
    InvalidEmail,

    // A request was made too recently; nothing was sent this time
    CooldownActive,

    // A code was wrong, missing or expired, or the change could not be completed. Deliberately
    // not told apart, so the answer never says which code failed or whether the address is taken.
    InvalidCode,

    // Too many wrong guesses; the request is cancelled and a new one must be made
    TooManyAttempts,

    // No change is pending, or it has expired
    NoPendingRequest
}
