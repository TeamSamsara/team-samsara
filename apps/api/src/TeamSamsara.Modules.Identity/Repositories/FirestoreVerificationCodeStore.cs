// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Repositories/FirestoreVerificationCodeStore.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : Firestore implementation of the verification code store.

using Google.Cloud.Firestore;
using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Repositories;

public class FirestoreVerificationCodeStore : IVerificationCodeStore
{
    #region Fields

    private readonly FirestoreDb _firestoreDb;

    #endregion

    #region Constructors

    // Initialize the store with its Firestore database
    public FirestoreVerificationCodeStore(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }

    #endregion

    #region Public Methods

    // Retrieves the pending code for a member and purpose, or null if there is none
    public async Task<VerificationCode?> GetAsync(string userId, VerificationPurpose purpose)
    {
        var snapshot = await GetDocument(userId, purpose).GetSnapshotAsync();

        return snapshot.Exists ? snapshot.ConvertTo<VerificationCode>() : null;
    }

    // Saves a code, replacing any existing code for the same member and purpose
    public async Task SaveAsync(VerificationCode code)
    {
        await _firestoreDb
            .Collection(IdentityFirestoreCollections.VerificationCodes)
            .Document(code.Id)
            .SetAsync(code);
    }

    // Atomically adds one to the failed-attempt count and returns the new count
    public async Task<int?> IncrementFailedAttemptsAsync(string userId, VerificationPurpose purpose)
    {
        var document = GetDocument(userId, purpose);

        return await _firestoreDb.RunTransactionAsync(async transaction =>
        {
            var snapshot = await transaction.GetSnapshotAsync(document);

            if (!snapshot.Exists)
            {
                return (int?)null;
            }

            var attempts = snapshot.GetValue<int>(nameof(VerificationCode.FailedAttempts)) + 1;
            transaction.Update(document, nameof(VerificationCode.FailedAttempts), attempts);

            return (int?)attempts;
        });
    }

    // Deletes the pending code for a member and purpose
    public async Task DeleteAsync(string userId, VerificationPurpose purpose)
    {
        await GetDocument(userId, purpose).DeleteAsync();
    }

    #endregion

    #region Private Methods

    // Resolves the document reference for a member's code of a given purpose
    private DocumentReference GetDocument(string userId, VerificationPurpose purpose)
    {
        return _firestoreDb
            .Collection(IdentityFirestoreCollections.VerificationCodes)
            .Document(VerificationCode.BuildId(userId, purpose));
    }

    #endregion
}
