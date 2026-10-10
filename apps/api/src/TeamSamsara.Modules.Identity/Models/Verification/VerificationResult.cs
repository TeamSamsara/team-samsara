// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/VerificationResult.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : The outcome of checking a submitted verification code.

namespace TeamSamsara.Modules.Identity.Models;

public enum VerificationResult
{
    // The code matched and has been consumed
    Valid,

    // The code was wrong
    Invalid,

    // The code existed but has expired
    Expired,

    // Too many wrong guesses; the code has been burned and a new one must be requested
    TooManyAttempts,

    // No code is pending for this member and purpose
    NotFound
}
