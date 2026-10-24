// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/Fakes/InMemoryHandleStore.cs
// Version : 1.1.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : An in-memory handle store that shares its profiles with the tests and decides
// claims with the same rules as the real store.

using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Repositories;

namespace TeamSamsara.Modules.Identity.Tests.Fakes;

public class InMemoryHandleStore : IHandleStore
{
    #region Fields

    private readonly Dictionary<string, string> _owners = new();
    private readonly IProfileStore _profiles;

    #endregion

    #region Constructors

    // Initialize the store with the profile store it records handles on
    public InMemoryHandleStore(IProfileStore profiles)
    {
        _profiles = profiles;
    }

    #endregion

    #region Public Methods

    public Task<string?> GetOwnerAsync(string handle)
    {
        return Task.FromResult(_owners.TryGetValue(handle, out var owner) ? owner : null);
    }

    public async Task<HandleClaimResult> ClaimAsync(string userId, string handle, HandleChange? change)
    {
        var profile = await _profiles.GetByIdAsync(userId);
        var owner = _owners.GetValueOrDefault(handle);

        var status = HandleClaimRules.Judge(profile, owner, userId, handle, change);

        if (status != HandleClaimStatus.Success)
        {
            return new HandleClaimResult(status, null);
        }

        var member = profile!;

        if (HandleClaimRules.AlreadyHolds(member, owner, userId, handle))
        {
            return new HandleClaimResult(HandleClaimStatus.Success, member);
        }

        FreePrevious(member, userId);
        _owners[handle] = userId;

        var updated = await _profiles.ModifyAsync(userId, current => HandleClaimRules.Apply(current, handle, change));

        return new HandleClaimResult(HandleClaimStatus.Success, updated);
    }

    public async Task ReleaseAsync(string userId)
    {
        var profile = await _profiles.GetByIdAsync(userId);

        if (profile is not null)
        {
            FreePrevious(profile, userId);
        }
    }

    #endregion

    #region Private Methods

    // Frees the profile's current handle if this member holds it
    private void FreePrevious(Profile profile, string userId)
    {
        if (profile.Handle is not null
            && _owners.TryGetValue(profile.Handle, out var owner)
            && owner == userId)
        {
            _owners.Remove(profile.Handle);
        }
    }

    #endregion
}
