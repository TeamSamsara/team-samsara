// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Repositories/FirestoreHandleStore.cs
// Version : 1.1.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Firestore implementation of the handle store. Each handle is one
// document keyed by the handle, so Firestore itself keeps it unique. The claim rules
// come from HandleClaimRules and run in the same transaction as the write.

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

    // Reads the profile and the handle, lets the rules decide, then writes both in one
    // transaction. Firestore reruns it if either changed meanwhile.
    public async Task<HandleClaimResult> ClaimAsync(string userId, string handle, HandleChange? change)
    {
        var profileReference = ProfileReference(userId);
        var handleReference = HandleReference(handle);

        return await _firestoreDb.RunTransactionAsync(async transaction =>
        {
            var profile = await ReadProfileAsync(transaction, profileReference);
            var owner = await ReadOwnerAsync(transaction, handleReference);

            var status = HandleClaimRules.Judge(profile, owner, userId, handle, change);

            if (status != HandleClaimStatus.Success)
            {
                return new HandleClaimResult(status, null);
            }

            var member = profile!;

            if (!HandleClaimRules.AlreadyHolds(member, owner, userId, handle))
            {
                await WriteClaimAsync(transaction, member, userId, handle, change);
            }

            return new HandleClaimResult(HandleClaimStatus.Success, member);
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

    // The member's profile, or null if they have none
    private static async Task<Profile?> ReadProfileAsync(Transaction transaction, DocumentReference reference)
    {
        var snapshot = await transaction.GetSnapshotAsync(reference);

        return snapshot.Exists ? snapshot.ConvertTo<Profile>() : null;
    }

    // The uid of whoever holds the handle, or null if nobody does
    private static async Task<string?> ReadOwnerAsync(Transaction transaction, DocumentReference reference)
    {
        var snapshot = await transaction.GetSnapshotAsync(reference);

        return snapshot.Exists ? snapshot.GetValue<string>(OwnerField) : null;
    }

    // Frees the member's old handle, gives them the new one and saves the profile.
    // Every read happens before the first write.
    private async Task WriteClaimAsync(
        Transaction transaction,
        Profile profile,
        string userId,
        string handle,
        HandleChange? change)
    {
        var previousReference = await FindPreviousAsync(transaction, profile, userId, handle);

        if (previousReference is not null)
        {
            transaction.Delete(previousReference);
        }

        HandleClaimRules.Apply(profile, handle, change);

        transaction.Set(HandleReference(handle), new Dictionary<string, object> { [OwnerField] = userId });
        transaction.Set(ProfileReference(userId), profile);
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
