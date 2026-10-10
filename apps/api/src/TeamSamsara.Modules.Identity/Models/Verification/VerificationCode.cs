// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/VerificationCode.cs
// Version : 1.0.1
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : A pending verification code for one member and one purpose. Only a salted hash of
// the code is stored, never the code itself.

using Google.Cloud.Firestore;

namespace TeamSamsara.Modules.Identity.Models;

[FirestoreData]
public class VerificationCode
{
    // Document id: "{userId}_{purpose}", which enforces one active code per member per purpose
    [FirestoreDocumentId]
    public required string Id { get; init; }

    [FirestoreProperty]
    public required string UserId { get; init; }

    [FirestoreProperty]
    public required VerificationPurpose Purpose { get; init; }

    // Salted hash of the code
    [FirestoreProperty]
    public required string CodeHash { get; init; }

    // Random per-code salt, base64
    [FirestoreProperty]
    public required string Salt { get; init; }

    [FirestoreProperty]
    public required DateTimeOffset CreatedAt { get; init; }

    [FirestoreProperty]
    public required DateTimeOffset ExpiresAt { get; init; }

    // Wrong guesses so far; the code is burned once this exceeds the configured maximum
    [FirestoreProperty]
    public int FailedAttempts { get; set; }

    // Builds the document id for a member's code of a given purpose
    public static string BuildId(string userId, VerificationPurpose purpose)
    {
        return $"{userId}_{purpose}";
    }
}
