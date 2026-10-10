// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/Authentication/ISignInTracker.cs
// Version : 1.3.0
// Latest commit: feat/account-purge
// Author : Gerrah
// Purpose : Records which sign-ins have passed verification, and forgets them when a member signs out or their credentials change.

using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Handlers;

public interface ISignInTracker
{
    #region Public Methods

    // Marks the sign-in identified by its Firebase auth_time as verified for this member.
    public Task RecordVerifiedAsync(string userId, long authTime);

    // Marks the sign-in as verified in the same write as `prepare`, which changes the member and
    // may refuse by returning false, in which case it must leave the member untouched and no
    // sign-in is recorded. Throws if the member does not exist.
    public Task<bool> RecordVerifiedAsync(string userId, long authTime, Func<User, bool> prepare);

    // Forgets one verified sign-in; does nothing if the member or the sign-in is unknown.
    public Task RemoveVerifiedAsync(string userId, long authTime);

    // Forgets every verified sign-in of this member, so earlier tokens no longer count as Member.
    // Throws if the member does not exist.
    public Task ClearVerifiedAsync(string userId);

    #endregion
}
