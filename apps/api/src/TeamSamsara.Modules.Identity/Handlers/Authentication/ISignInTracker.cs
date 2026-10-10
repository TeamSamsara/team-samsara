// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/Authentication/ISignInTracker.cs
// Version : 1.2.0
// Latest commit: feat/logout
// Author : Gerrah
// Purpose : Records which sign-ins have passed verification, and forgets them when a member signs out or their credentials change.

namespace TeamSamsara.Modules.Identity.Handlers;

public interface ISignInTracker
{
    #region Public Methods

    // Marks the sign-in identified by its Firebase auth_time as verified for this member.
    public Task RecordVerifiedAsync(string userId, long authTime);

    // Forgets one verified sign-in; does nothing if the member or the sign-in is unknown.
    public Task RemoveVerifiedAsync(string userId, long authTime);

    // Forgets every verified sign-in of this member, so earlier tokens no longer count as Member.
    // Throws if the member does not exist.
    public Task ClearVerifiedAsync(string userId);

    #endregion
}
