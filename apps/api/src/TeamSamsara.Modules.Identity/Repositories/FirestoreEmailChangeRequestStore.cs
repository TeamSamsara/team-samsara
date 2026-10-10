// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Repositories/FirestoreEmailChangeRequestStore.cs
// Version : 1.0.0
// Latest commit: feat/email-change-request-store
// Author : Gerrah
// Purpose : Firestore implementation of the email change request store.

using Google.Cloud.Firestore;
using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Repositories;

public class FirestoreEmailChangeRequestStore : IEmailChangeRequestStore
{
    #region Fields

    private readonly FirestoreDb _firestoreDb;

    #endregion

    #region Constructors

    public FirestoreEmailChangeRequestStore(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }

    #endregion

    #region Public Methods

    // Returns the member's pending change, or null if there is none
    public async Task<EmailChangeRequest?> GetAsync(string userId)
    {
        var snapshot = await GetDocument(userId).GetSnapshotAsync();

        return snapshot.Exists ? snapshot.ConvertTo<EmailChangeRequest>() : null;
    }

    // Saves a request, replacing any pending one for the same member
    public async Task SaveAsync(EmailChangeRequest request)
    {
        await GetDocument(request.Id).SetAsync(request);
    }

    // Marks one code as confirmed inside a transaction, doing nothing if the request is gone
    public async Task MarkVerifiedAsync(string userId, VerificationPurpose purpose)
    {
        var document = GetDocument(userId);
        var field = GetVerifiedField(purpose);

        await _firestoreDb.RunTransactionAsync(async transaction =>
        {
            var snapshot = await transaction.GetSnapshotAsync(document);

            if (snapshot.Exists)
            {
                transaction.Update(document, field, true);
            }
        });
    }

    // Reads and deletes the request in one transaction, so it can only be taken once
    public async Task<EmailChangeRequest?> TakeAsync(string userId)
    {
        var document = GetDocument(userId);

        return await _firestoreDb.RunTransactionAsync(async transaction =>
        {
            var snapshot = await transaction.GetSnapshotAsync(document);

            if (!snapshot.Exists)
            {
                return (EmailChangeRequest?)null;
            }

            transaction.Delete(document);

            return snapshot.ConvertTo<EmailChangeRequest>();
        });
    }

    // Deletes the member's pending change
    public async Task DeleteAsync(string userId)
    {
        await GetDocument(userId).DeleteAsync();
    }

    #endregion

    #region Private Methods

    // Resolves the document reference for a member's pending change
    private DocumentReference GetDocument(string userId)
    {
        return _firestoreDb
            .Collection(IdentityFirestoreCollections.EmailChangeRequests)
            .Document(userId);
    }

    // Maps an email-change purpose to the flag that records its confirmation
    private static string GetVerifiedField(VerificationPurpose purpose)
    {
        return purpose switch
        {
            VerificationPurpose.EmailChangeOld => nameof(EmailChangeRequest.OldCodeVerified),
            VerificationPurpose.EmailChangeNew => nameof(EmailChangeRequest.NewCodeVerified),
            _ => throw new ArgumentOutOfRangeException(
                nameof(purpose), purpose, "Not an email change purpose.")
        };
    }

    #endregion
}
