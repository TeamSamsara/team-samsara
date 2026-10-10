// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Repositories/FirestoreProfileStore.cs
// Version : 1.1.0
// Latest commit: fix/profile-atomic-update
// Author : Gerrah
// Purpose : Firestore implementation of the profile store.

using Google.Cloud.Firestore;
using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Repositories;

public class FirestoreProfileStore : IProfileStore
{
    #region Fields

    private readonly FirestoreDb _firestoreDb;

    #endregion

    #region Constructors

    // Initialize the store with its Firestore database
    public FirestoreProfileStore(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }

    #endregion

    #region Public Methods

    // Retrieves a profile by Firebase uid, or null if the member has no profile
    public async Task<Profile?> GetByIdAsync(string id)
    {
        var snapshot = await _firestoreDb
            .Collection(IdentityFirestoreCollections.Profiles)
            .Document(id)
            .GetSnapshotAsync();

        return snapshot.Exists ? snapshot.ConvertTo<Profile>() : null;
    }

    // Creates a new profile record (fails if one already exists for that uid)
    public async Task CreateAsync(Profile profile)
    {
        await _firestoreDb
            .Collection(IdentityFirestoreCollections.Profiles)
            .Document(profile.Id)
            .CreateAsync(profile);
    }

    // Reads, changes and saves the profile in one transaction; Firestore reruns it if the record changed meanwhile
    public async Task<Profile?> ModifyAsync(string id, Action<Profile> modify)
    {
        var reference = _firestoreDb
            .Collection(IdentityFirestoreCollections.Profiles)
            .Document(id);

        return await _firestoreDb.RunTransactionAsync<Profile?>(async transaction =>
        {
            var snapshot = await transaction.GetSnapshotAsync(reference);

            if (!snapshot.Exists)
            {
                return null;
            }

            var profile = snapshot.ConvertTo<Profile>();
            modify(profile);
            transaction.Set(reference, profile);

            return profile;
        });
    }

    // Deletes a profile record by id
    public async Task DeleteAsync(string id)
    {
        await _firestoreDb
            .Collection(IdentityFirestoreCollections.Profiles)
            .Document(id)
            .DeleteAsync();
    }

    #endregion
}
