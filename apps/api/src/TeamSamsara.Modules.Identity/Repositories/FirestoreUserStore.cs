// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Repositories/FirestoreUserStore.cs
// Version : 1.2.0
// Latest commit: feat/account-purge
// Author : Gerrah
// Purpose : Firestore implementation of the user store.

using Google.Cloud.Firestore;
using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Repositories;

public class FirestoreUserStore : IUserStore
{
    #region Fields

    private readonly FirestoreDb _firestoreDb;

    #endregion

    #region Constructors

    // Initialize the store with its Firestore database
    public FirestoreUserStore(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }

    #endregion

    #region Public Methods

    // Retrieves a user by Firebase uid, or null if no record exists for that uid
    public async Task<User?> GetByIdAsync(string id)
    {
        var snapshot = await _firestoreDb
            .Collection(IdentityFirestoreCollections.Users)
            .Document(id)
            .GetSnapshotAsync();

        return snapshot.Exists ? snapshot.ConvertTo<User>() : null;
    }

    // Creates a new user record (fails if one already exists for that uid)
    public async Task CreateAsync(User user)
    {
        await _firestoreDb
            .Collection(IdentityFirestoreCollections.Users)
            .Document(user.Id)
            .CreateAsync(user);
    }

    // Saves the current state of an existing user record
    public async Task UpdateAsync(User user)
    {
        await _firestoreDb
            .Collection(IdentityFirestoreCollections.Users)
            .Document(user.Id)
            .SetAsync(user);
    }

    // Reads, changes and saves the user inside one Firestore transaction
    public async Task ModifyAsync(string id, Action<User> modify)
    {
        var reference = _firestoreDb
            .Collection(IdentityFirestoreCollections.Users)
            .Document(id);

        await _firestoreDb.RunTransactionAsync(async transaction =>
        {
            var snapshot = await transaction.GetSnapshotAsync(reference);

            if (!snapshot.Exists)
            {
                throw new InvalidOperationException($"User '{id}' does not exist.");
            }

            var user = snapshot.ConvertTo<User>();
            modify(user);
            transaction.Set(reference, user);
        });
    }

    // Retrieves every user whose deletion marker is older than the cutoff
    public async Task<IReadOnlyList<User>> ListDeletedBeforeAsync(DateTimeOffset cutoff)
    {
        var snapshot = await _firestoreDb
            .Collection(IdentityFirestoreCollections.Users)
            .WhereLessThan(nameof(User.DeletedAt), Timestamp.FromDateTimeOffset(cutoff))
            .GetSnapshotAsync();

        return snapshot.Documents.Select(document => document.ConvertTo<User>()).ToList();
    }

    // Claims a deleted account for purging inside one transaction, so only one instance can hold it
    public async Task<bool> TryClaimForPurgeAsync(
        string id,
        DateTimeOffset cutoff,
        DateTimeOffset now,
        DateTimeOffset leaseUntil)
    {
        var reference = _firestoreDb
            .Collection(IdentityFirestoreCollections.Users)
            .Document(id);

        return await _firestoreDb.RunTransactionAsync(async transaction =>
        {
            var snapshot = await transaction.GetSnapshotAsync(reference);

            if (!snapshot.Exists)
            {
                return false;
            }

            var user = snapshot.ConvertTo<User>();

            if (user.DeletedAt is not { } deletedAt || deletedAt >= cutoff)
            {
                return false;
            }

            if (user.PurgeLeaseUntil is { } heldUntil && heldUntil > now)
            {
                return false;
            }

            transaction.Update(
                reference,
                nameof(User.PurgeLeaseUntil),
                Timestamp.FromDateTimeOffset(leaseUntil));

            return true;
        });
    }

    // Deletes a user record by id
    public async Task DeleteAsync(string id)
    {
        await _firestoreDb
            .Collection(IdentityFirestoreCollections.Users)
            .Document(id)
            .DeleteAsync();
    }

    #endregion
}
