// File: /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/Fakes/ThrowingProfileStore.cs
// Version: 1.0.0
// Latest commit: feature/identity-profile
// Author: Gerrah
//
// Purpose: A profile store wrapper that can be made to fail saving, as a database outage would.

using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Repositories;

namespace TeamSamsara.Modules.Identity.Tests.Fakes;

public class ThrowingProfileStore : IProfileStore
{
    #region Fields

    private readonly IProfileStore _inner;

    #endregion

    #region Constructors

    public ThrowingProfileStore(IProfileStore inner)
    {
        _inner = inner;
    }

    #endregion

    #region Properties

    // Makes UpdateAsync throw instead of saving
    public bool FailUpdates { get; set; }

    #endregion

    #region Public Methods

    public Task<Profile?> GetByIdAsync(string id) => _inner.GetByIdAsync(id);

    public Task CreateAsync(Profile profile) => _inner.CreateAsync(profile);

    public Task UpdateAsync(Profile profile)
    {
        if (FailUpdates)
        {
            throw new InvalidOperationException("Update failed.");
        }

        return _inner.UpdateAsync(profile);
    }

    public Task DeleteAsync(string id) => _inner.DeleteAsync(id);

    #endregion
}
