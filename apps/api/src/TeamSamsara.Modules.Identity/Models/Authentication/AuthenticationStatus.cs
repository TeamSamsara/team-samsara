// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/AuthenticationStatus.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : The outcome of an authentication step. Endpoints translate these into responses.

namespace TeamSamsara.Modules.Identity.Models;

public enum AuthenticationStatus
{
    // The sign-in is verified and the member may use the API
    Authenticated,

    // The sign-in comes from an unrecognized client (or a deleted account is being restored);
    // a code has been sent and must be confirmed
    ChallengeRequired,

    // A code was sent too recently; nothing was sent this time
    CooldownActive,

    // The account exists but registration was never completed; the client should send the
    // person back to registration confirmation
    RegistrationIncomplete,

    // No usable account exists for this caller (never recorded, or past the recovery window)
    AccountNotFound,

    // The submitted code was wrong
    InvalidCode,

    // The submitted code has expired
    CodeExpired,

    // Too many wrong guesses; a new code must be requested
    TooManyAttempts,

    // No code is pending for this account
    NoPendingCode
}
