// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Repositories/IProfileStore.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : Looks up, records, updates and removes member profile (display) records,
// independent of where they are stored. Keyed by Firebase uid.

using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Repositories;

public interface IProfileStore
{
    #region Public Methods

    // Retrieves a profile by Firebase uid, or null if the member has no profile
    public Task<Profile?> GetByIdAsync(string id);

    // Creates a new profile record
    public Task CreateAsync(Profile profile);

    // Saves the current state of an existing profile record
    public Task UpdateAsync(Profile profile);

    // Deletes a profile record by id
    public Task DeleteAsync(string id);

    #endregion
}
