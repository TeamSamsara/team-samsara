// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/KnownFingerprint.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : A device/IP fingerprint a member has successfully logged in from. Only a hash is
// stored, never the raw IP or user agent.

using Google.Cloud.Firestore;

namespace TeamSamsara.Modules.Identity.Models;

[FirestoreData]
public class KnownFingerprint
{
    // Hash of the IP address plus user agent
    [FirestoreProperty]
    public required string Hash { get; init; }

    // When this fingerprint last completed a successful login
    [FirestoreProperty]
    public required DateTimeOffset LastSeenAt { get; set; }
}
