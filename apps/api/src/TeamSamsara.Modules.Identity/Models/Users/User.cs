// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/User.cs
// Version : 1.2.0
// Latest commit: feat/account-purge
// Author : Gerrah
// Purpose : The security/identity record for an account.
// Distinct from Profile (display data) and from the Firebase Auth account (credentials).

using Google.Cloud.Firestore;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Models;

[FirestoreData]
public class User
{
    // The Firebase uid, which is also the Firestore document id
    [FirestoreDocumentId]
    public required string Id { get; init; }

    // Guest until registration is completed, then Member (Admin is reserved for later)
    [FirestoreProperty]
    public required AccessLevel AccessLevel { get; set; }

    [FirestoreProperty]
    public required DateTimeOffset CreatedAt { get; init; }

    // Set when the account is deleted; cleared if the member logs back in within the 30-day
    // recovery window. Null means the account is active.
    [FirestoreProperty]
    public DateTimeOffset? DeletedAt { get; set; }

    // Set while one instance is purging this deleted account, so no other instance purges it and
    // no login restores it meanwhile; a crashed purge simply lets it lapse and is retried.
    [FirestoreProperty]
    public DateTimeOffset? PurgeLeaseUntil { get; set; }

    // Fingerprints the member has logged in from before. A login from an unknown one triggers
    // the new-device challenge.
    [FirestoreProperty]
    public List<KnownFingerprint> KnownFingerprints { get; set; } = new();

    // Firebase auth_time values (unix seconds) of sign-ins that passed verification. A token is
    // only honored as Member when its auth_time is in this list; the oldest are trimmed.
    [FirestoreProperty]
    public List<long> VerifiedSignIns { get; set; } = new();
}
