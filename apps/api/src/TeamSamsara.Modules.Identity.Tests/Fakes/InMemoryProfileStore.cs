// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/Fakes/InMemoryProfileStore.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : An in-memory profile store.

using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Repositories;

namespace TeamSamsara.Modules.Identity.Tests.Fakes;

public class InMemoryProfileStore : IProfileStore
{
    #region Fields

    private readonly Dictionary<string, Profile> _profiles = new();

    #endregion

    #region Public Methods

    public Task<Profile?> GetByIdAsync(string id)
    {
        return Task.FromResult(_profiles.TryGetValue(id, out var profile) ? Copy(profile) : null);
    }

    public Task CreateAsync(Profile profile)
    {
        if (!_profiles.TryAdd(profile.Id, Copy(profile)))
        {
            throw new InvalidOperationException($"Profile '{profile.Id}' already exists.");
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Profile profile)
    {
        _profiles[profile.Id] = Copy(profile);

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id)
    {
        _profiles.Remove(id);

        return Task.CompletedTask;
    }

    #endregion

    #region Private Methods

    private static Profile Copy(Profile profile)
    {
        return new Profile
        {
            Id = profile.Id,
            DisplayName = profile.DisplayName,
            Bio = profile.Bio,
            ProfilePictureAssetId = profile.ProfilePictureAssetId,
            BannerAssetId = profile.BannerAssetId
        };
    }

    #endregion
}
