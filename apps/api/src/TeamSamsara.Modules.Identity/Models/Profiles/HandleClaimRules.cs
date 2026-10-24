// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/Profiles/HandleClaimRules.cs
// Version : 1.0.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : The rules of giving a handle to a member, shared by every handle store so
// they all decide the same way: who may take it, the rename cooldown, what gets saved.

namespace TeamSamsara.Modules.Identity.Models;

public static class HandleClaimRules
{
    #region Public Methods

    // Decides whether the member may take the handle, from the profile and the handle's
    // current owner. Success also covers a member who already holds the handle.
    public static HandleClaimStatus Judge(
        Profile? profile,
        string? owner,
        string userId,
        string handle,
        HandleChange? change)
    {
        return profile is null ? HandleClaimStatus.ProfileNotFound
            : IsHeldByAnother(owner, userId) ? HandleClaimStatus.HandleTaken
            : AlreadyHolds(profile, owner, userId, handle) ? HandleClaimStatus.Success
            : IsCoolingDown(profile, change) ? HandleClaimStatus.HandleCooldown
            : HandleClaimStatus.Success;
    }

    // True when the member already holds this handle, so claiming it again changes nothing
    public static bool AlreadyHolds(Profile profile, string? owner, string userId, string handle)
    {
        return profile.Handle == handle && owner == userId;
    }

    // Puts the handle on the profile. The change time is saved only for a member's own
    // change made after onboarding, so choosing a handle while signing up costs nothing.
    public static void Apply(Profile profile, string handle, HandleChange? change)
    {
        profile.Handle = handle;

        if (change is not null && profile.OnboardingCompleted)
        {
            profile.HandleChangedAt = change.Now;
        }
    }

    #endregion

    #region Private Methods

    private static bool IsHeldByAnother(string? owner, string userId)
    {
        return owner is not null && owner != userId;
    }

    // True when this is the member's own change and they changed their handle more recently
    // than the cooldown allows. A handle assigned by the system is never held back.
    private static bool IsCoolingDown(Profile profile, HandleChange? change)
    {
        return change is not null
            && change.CooldownDays > 0
            && profile.HandleChangedAt is { } changedAt
            && change.Now < changedAt.AddDays(change.CooldownDays);
    }

    #endregion
}
