// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Repositories/IProfileStore.cs
// Version : 1.1.0
// Latest commit: fix/profile-atomic-update
// Author : Gerrah
// Purpose : Looks up, records, changes and removes member profile (display) records,
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

    // Applies a change to the profile as it is when saved, so a change made meanwhile is never overwritten.
    // The change may run more than once if the store has to retry. Returns the saved profile, or null
    // (saving nothing) if the member has no profile.
    public Task<Profile?> ModifyAsync(string id, Action<Profile> modify);

    // Deletes a profile record by id
    public Task DeleteAsync(string id);

    #endregion
}
