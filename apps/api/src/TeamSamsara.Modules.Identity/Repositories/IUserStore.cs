// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Repositories/IUserStore.cs
// Version : 1.2.0
// Latest commit: feat/account-purge
// Author : Gerrah
// Purpose : Looks up, records, updates, lists and removes user (security/identity) records,
// independent of where they are stored. Keyed by Firebase uid.

using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Repositories;

public interface IUserStore
{
    #region Public Methods

    // Retrieves a user by Firebase uid, or null if no record exists for that uid
    public Task<User?> GetByIdAsync(string id);

    // Creates a new user record
    public Task CreateAsync(User user);

    // Saves the current state of an existing user record (access level, deletion marker,
    // known fingerprints, verified sign-ins)
    public Task UpdateAsync(User user);

    // Reads the user, applies the change and saves it as one atomic step, so concurrent
    // changes (two sign-ins at once) cannot overwrite each other. Throws if the user is missing.
    public Task ModifyAsync(string id, Action<User> modify);

    // Retrieves every user whose deletion marker is older than the cutoff - the accounts whose
    // 30-day recovery window has expired and are due for purging
    public Task<IReadOnlyList<User>> ListDeletedBeforeAsync(DateTimeOffset cutoff);

    // Atomically claims a deleted account for purging: true only if the user still exists, is
    // marked deleted before the cutoff and is not held by another purge, and then holds it until
    // leaseUntil. False means someone restored it, removed it or is purging it already.
    public Task<bool> TryClaimForPurgeAsync(
        string id,
        DateTimeOffset cutoff,
        DateTimeOffset now,
        DateTimeOffset leaseUntil);

    // Deletes a user record by id
    public Task DeleteAsync(string id);

    #endregion
}
