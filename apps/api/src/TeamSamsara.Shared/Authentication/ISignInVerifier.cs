// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Authentication/ISignInVerifier.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : Lets the authentication handler ask whether a sign-in has passed the member
// verification (login check and, on an unfamiliar device, the challenge). Defined in Shared and
// implemented by Identity, so Shared never depends on a module.

namespace TeamSamsara.Shared.Authentication;

public interface ISignInVerifier
{
    #region Public Methods

    // Whether the sign-in identified by `authTime` (Unix seconds, from the token's auth_time
    // claim) has been verified for this member. False for sign-ins that have not, and for
    // accounts that are unknown or deleted.
    public Task<bool> IsVerifiedAsync(string userId, long authTime);

    #endregion
}
