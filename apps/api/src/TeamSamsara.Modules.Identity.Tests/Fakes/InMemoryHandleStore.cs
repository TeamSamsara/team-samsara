// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/Fakes/InMemoryHandleStore.cs
// Version : 1.0.0
// Latest commit: task/api-profile-onboarding
// Author : Gerrah
// Purpose : An in-memory handle store that shares its profiles with the tests.

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

    public async Task<HandleClaimStatus> ClaimAsync(string userId, string handle, DateTimeOffset? changedAt)
    {
        var profile = await _profiles.GetByIdAsync(userId);

        if (profile is null)
        {
            return HandleClaimStatus.ProfileNotFound;
        }

        if (_owners.TryGetValue(handle, out var owner) && owner != userId)
        {
            return HandleClaimStatus.HandleTaken;
        }

        if (profile.Handle == handle && owner == userId)
        {
            return HandleClaimStatus.Success;
        }

        FreePrevious(profile, userId);
        _owners[handle] = userId;

        await _profiles.ModifyAsync(userId, current =>
        {
            current.Handle = handle;

            if (changedAt is not null)
            {
                current.HandleChangedAt = changedAt;
            }
        });

        return HandleClaimStatus.Success;
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
