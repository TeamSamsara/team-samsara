// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/Passwords/PasswordResetToken.cs
// Version : 1.0.0
// Latest commit: feat/password-reset-token-store
// Author : Gerrah
// Purpose : A pending permission to choose a new password, granted once the member has confirmed
// the emailed reset code. Only the hash of the token is stored, never the token itself.

using Google.Cloud.Firestore;

namespace TeamSamsara.Modules.Identity.Models;

[FirestoreData]
public class PasswordResetToken
{
    // Document id: the hash of the token the member holds. Looking a token up is a direct
    // read by id, and the token itself never touches the database.
    [FirestoreDocumentId]
    public required string Id { get; init; }

    [FirestoreProperty]
    public required string UserId { get; init; }

    [FirestoreProperty]
    public required DateTimeOffset CreatedAt { get; init; }

    [FirestoreProperty]
    public required DateTimeOffset ExpiresAt { get; init; }
}
