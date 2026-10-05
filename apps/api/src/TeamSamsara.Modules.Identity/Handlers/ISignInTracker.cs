// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/ISignInTracker.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : Records which sign-ins have passed verification. The matching check
// (ISignInVerifier) is what the authentication handler uses on every request.

namespace TeamSamsara.Modules.Identity.Handlers;

public interface ISignInTracker
{
    #region Public Methods

    // Marks the sign-in identified by its Firebase auth_time as verified for this member
    public Task RecordVerifiedAsync(string userId, long authTime);

    #endregion
}
