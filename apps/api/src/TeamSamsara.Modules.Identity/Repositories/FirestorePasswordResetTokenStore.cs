// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Repositories/FirestorePasswordResetTokenStore.cs
// Version : 1.0.0
// Latest commit: feat/password-reset-token-store
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
