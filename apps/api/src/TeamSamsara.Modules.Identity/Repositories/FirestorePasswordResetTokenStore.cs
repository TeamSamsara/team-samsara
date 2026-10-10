// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Repositories/FirestorePasswordResetTokenStore.cs
// Version : 1.1.0
// Latest commit: feat/account-purge
// Author : Gerrah
// Purpose : Firestore implementation of the password reset token store.

using Google.Cloud.Firestore;
using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Repositories;

public class FirestorePasswordResetTokenStore : IPasswordResetTokenStore
{
    #region Fields

    private readonly FirestoreDb _firestoreDb;

    #endregion

    #region Constructors

    // Initialize the store with its Firestore database
    public FirestorePasswordResetTokenStore(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }

    #endregion

    #region Public Methods

    // Saves a token record
    public async Task SaveAsync(PasswordResetToken token)
    {
        await GetDocument(token.Id).SetAsync(token);
    }

    // Reads and deletes the record in one transaction, so a token can only ever be taken once
    public async Task<PasswordResetToken?> TakeAsync(string id)
    {
        var document = GetDocument(id);

        return await _firestoreDb.RunTransactionAsync(async transaction =>
        {
            var snapshot = await transaction.GetSnapshotAsync(document);

            if (!snapshot.Exists)
            {
                return (PasswordResetToken?)null;
            }

            transaction.Delete(document);

            return snapshot.ConvertTo<PasswordResetToken>();
        });
    }

    // Deletes every token of this member in one batch; a member only ever has a handful, short-lived
    public async Task DeleteForUserAsync(string userId)
    {
        var snapshot = await _firestoreDb
            .Collection(IdentityFirestoreCollections.PasswordResetTokens)
            .WhereEqualTo(nameof(PasswordResetToken.UserId), userId)
            .GetSnapshotAsync();

        if (snapshot.Count == 0)
        {
            return;
        }

        var batch = _firestoreDb.StartBatch();

        foreach (var document in snapshot.Documents)
        {
            batch.Delete(document.Reference);
        }

        await batch.CommitAsync();
    }

    #endregion

    #region Private Methods

    // Resolves the document reference for a token record
    private DocumentReference GetDocument(string id)
    {
        return _firestoreDb
            .Collection(IdentityFirestoreCollections.PasswordResetTokens)
            .Document(id);
    }

    #endregion
}
