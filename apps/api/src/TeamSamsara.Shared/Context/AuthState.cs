// File: /team-samsara/apps/api/src/TeamSamsara.Shared/Context/AuthState.cs
// Version: 1.0.0
// Latest commit: feature/identity-profile
// Author: Gerrah
//
// Purpose: The reason a caller is or is not allowed through member-only endpoints.

namespace TeamSamsara.Shared.Context;

public enum AuthState
{
    // No token, or an invalid one. Nobody can be identified (401)
    Anonymous,

    // Known token, but the user never confirmed their email address (403)
    RegistrationIncomplete,

    // Known token and confirmed email, but the sign-in is not verified yet (403)
    // Example: a new-device challenge is pending
    VerificationRequired,

    // Known token, confirmed email and verified sign-in (200)
    Member
}
