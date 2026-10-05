// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/RegistrationStatus.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : The outcome of a registration step. Endpoints translate these into responses.

namespace TeamSamsara.Modules.Identity.Models;

public enum RegistrationStatus
{
    // The step succeeded (code sent, or registration completed)
    Success,

    // The member has already completed registration
    AlreadyRegistered,

    // No usable account exists for this caller (missing in Firebase, or registration was
    // never started)
    AccountNotFound,

    // A code was sent too recently; nothing was sent this time
    CooldownActive,

    // The submitted code was wrong
    InvalidCode,

    // The submitted code has expired
    CodeExpired,

    // Too many wrong guesses; a new code must be requested
    TooManyAttempts,

    // No code is pending for this account
    NoPendingCode
}
