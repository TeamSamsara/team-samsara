// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Repositories/FirestoreHandleStore.cs
// Version : 1.0.0
// Latest commit: task/api-profile-onboarding
// Author : Gerrah
// Purpose : Firestore implementation of the handle store. Each handle is one
// document keyed by the handle, so Firestore itself keeps it unique.

using Google.Cloud.Firestore;
using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Repositories;

public class FirestoreHandleStore : IHandleStore
{
    #region Fields

    private const string OwnerField = "userId";

    private readonly FirestoreDb _firestoreDb;

    #endregion

    #region Constructors

    // Initialize the store with its Firestore database
    public FirestoreHandleStore(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }

    #endregion

    #region Public Methods

    // Returns the uid of the member who holds the handle, or null if nobody does
    public async Task<string?> GetOwnerAsync(string handle)
    {
        var snapshot = await HandleReference(handle).GetSnapshotAsync();

        return snapshot.Exists ? snapshot.GetValue<string>(OwnerField) : null;
    }

    // Reads the handle and the profile, then writes both in one transaction.
    // Firestore reruns it if either changed meanwhile.
    public async Task<HandleClaimStatus> ClaimAsync(string userId, string handle, DateTimeOffset? changedAt)
    {
        var handleReference = HandleReference(handle);
        var profileReference = ProfileReference(userId);

        return await _firestoreDb.RunTransactionAsync(async transaction =>
        {
            var profileSnapshot = await transaction.GetSnapshotAsync(profileReference);

            if (!profileSnapshot.Exists)
            {
                return HandleClaimStatus.ProfileNotFound;
            }

            var handleSnapshot = await transaction.GetSnapshotAsync(handleReference);

            if (handleSnapshot.Exists && handleSnapshot.GetValue<string>(OwnerField) != userId)
            {
                return HandleClaimStatus.HandleTaken;
            }

            var profile = profileSnapshot.ConvertTo<Profile>();

            if (profile.Handle == handle && handleSnapshot.Exists)
            {
                return HandleClaimStatus.Success;
            }

            // All reads must come before the first write
            var previousReference = await FindPreviousAsync(transaction, profile, userId, handle);

            if (previousReference is not null)
            {
                transaction.Delete(previousReference);
            }

            profile.Handle = handle;

            if (changedAt is not null)
            {
                profile.HandleChangedAt = changedAt;
            }

            transaction.Set(handleReference, new Dictionary<string, object> { [OwnerField] = userId });
            transaction.Set(profileReference, profile);

            return HandleClaimStatus.Success;
        });
    }

    // Deletes the handle document of the handle on the member's profile, if the member holds it
    public async Task ReleaseAsync(string userId)
    {
        var profileReference = ProfileReference(userId);

        await _firestoreDb.RunTransactionAsync(async transaction =>
        {
            var profileSnapshot = await transaction.GetSnapshotAsync(profileReference);

            if (!profileSnapshot.Exists)
            {
                return;
            }

            var handle = profileSnapshot.ConvertTo<Profile>().Handle;

            if (handle is null)
            {
                return;
            }

            var handleReference = HandleReference(handle);
            var handleSnapshot = await transaction.GetSnapshotAsync(handleReference);

            if (handleSnapshot.Exists && handleSnapshot.GetValue<string>(OwnerField) == userId)
            {
                transaction.Delete(handleReference);
            }
        });
    }

    #endregion

    #region Private Methods

    private DocumentReference HandleReference(string handle)
    {
        return _firestoreDb.Collection(IdentityFirestoreCollections.Handles).Document(handle);
    }

    private DocumentReference ProfileReference(string userId)
    {
        return _firestoreDb.Collection(IdentityFirestoreCollections.Profiles).Document(userId);
    }

    // The member's current handle document when they are moving to a different handle and
    // still hold the old one, so a record belonging to someone else is never deleted
    private async Task<DocumentReference?> FindPreviousAsync(
        Transaction transaction,
        Profile profile,
        string userId,
        string newHandle)
    {
        if (profile.Handle is null || profile.Handle == newHandle)
        {
            return null;
        }

        var previousReference = HandleReference(profile.Handle);
        var previousSnapshot = await transaction.GetSnapshotAsync(previousReference);

        var holdsPrevious = previousSnapshot.Exists && previousSnapshot.GetValue<string>(OwnerField) == userId;

        return holdsPrevious ? previousReference : null;
    }

    #endregion
}
