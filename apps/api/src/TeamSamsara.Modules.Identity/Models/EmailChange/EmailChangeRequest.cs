// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/EmailChange/EmailChangeRequest.cs
// Version : 1.0.0
// Latest commit: feat/email-change-request-store
// Author : Gerrah
// Purpose : A member's pending email change: the address they asked for and which codes are confirmed.

using Google.Cloud.Firestore;

namespace TeamSamsara.Modules.Identity.Models;

[FirestoreData]
public class EmailChangeRequest
{
    // Document id: the member's id, which allows one pending change per member
    [FirestoreDocumentId]
    public required string Id { get; init; }

    // The address the member asked to change to, fixed when the request is made
    [FirestoreProperty]
    public required string NewEmail { get; init; }

    [FirestoreProperty]
    public required DateTimeOffset CreatedAt { get; init; }

    [FirestoreProperty]
    public required DateTimeOffset ExpiresAt { get; init; }

    // The code sent to the current address has been confirmed
    [FirestoreProperty]
    public bool OldCodeVerified { get; set; }

    // The code sent to the new address has been confirmed
    [FirestoreProperty]
    public bool NewCodeVerified { get; set; }
}
