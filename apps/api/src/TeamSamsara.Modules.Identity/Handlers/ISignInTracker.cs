// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/ISignInTracker.cs
// Version : 1.1.0
// Latest commit: fix/password-change-verified-sign-ins
// Author : Gerrah
// Purpose : Records which sign-ins have passed verification, and forgets them all when a member's
// credentials change. The matching check (ISignInVerifier) is what the authentication handler
// uses on every request.

namespace TeamSamsara.Modules.Identity.Handlers;

public interface ISignInTracker
{
    #region Public Methods

    // Marks the sign-in identified by its Firebase auth_time as verified for this member
    public Task RecordVerifiedAsync(string userId, long authTime);

    // Forgets every verified sign-in of this member, so tokens issued before now no longer
    // count as Member. Throws if the member does not exist.
    public Task ClearVerifiedAsync(string userId);

    #endregion
}
