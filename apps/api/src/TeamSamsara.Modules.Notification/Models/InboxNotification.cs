// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/Models/InboxNotification.cs
// Version : 1.0.0
// Latest commit: feat/notification-send-path
// Author : Gerrah
// Purpose : One notification in a member's inbox: what happened (Key and Data), when, and whether it was read.

using System.Security.Cryptography;
using System.Text;
using Google.Cloud.Firestore;

namespace TeamSamsara.Modules.Notification.Models;

[FirestoreData]
public class InboxNotification
{
    #region Fields

    private const char DedupeSeparator = '\0';

    #endregion

    #region Properties

    // Document id: 32 hex characters, or 64 when it was built from a dedupe key
    [FirestoreDocumentId]
    public required string Id { get; init; }

    // Firebase uid of the member the notification is for
    [FirestoreProperty]
    public required string RecipientId { get; init; }

    // What happened, as "category.event"; the frontend turns it into text
    [FirestoreProperty]
    public required string Key { get; init; }

    // Values the frontend fills into the text for Key
    [FirestoreProperty]
    public required Dictionary<string, string> Data { get; init; }

    [FirestoreProperty]
    public required DateTimeOffset CreatedAt { get; init; }

    // When the Firestore TTL policy may delete the notification
    [FirestoreProperty]
    public required DateTimeOffset ExpiresAt { get; init; }

    // When the member read it, or null while unread
    [FirestoreProperty]
    public DateTimeOffset? ReadAt { get; set; }

    #endregion

    #region Public Methods

    // Builds the document id for a recipient's dedupe key, so a repeated request lands on the same document
    public static string BuildDedupeId(string recipientId, string dedupeKey)
    {
        var bytes = Encoding.UTF8.GetBytes($"{recipientId}{DedupeSeparator}{dedupeKey}");

        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }

    #endregion
}
