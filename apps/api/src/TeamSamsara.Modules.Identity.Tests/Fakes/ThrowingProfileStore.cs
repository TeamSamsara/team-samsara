// File: /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/Fakes/ThrowingProfileStore.cs
// Version: 1.1.0
// Latest commit: fix/profile-atomic-update
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

    // Makes ModifyAsync throw instead of saving
    public bool FailUpdates { get; set; }

    #endregion

    #region Public Methods

    public Task<Profile?> GetByIdAsync(string id) => _inner.GetByIdAsync(id);

    public Task CreateAsync(Profile profile) => _inner.CreateAsync(profile);

    public Task<Profile?> ModifyAsync(string id, Action<Profile> modify)
    {
        if (FailUpdates)
        {
            throw new InvalidOperationException("Update failed.");
        }

        return _inner.ModifyAsync(id, modify);
    }

    public Task DeleteAsync(string id) => _inner.DeleteAsync(id);

    #endregion
}
