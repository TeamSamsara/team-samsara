// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Assets/Models/AssetMetadata.cs
// Version : 1.0.1
// Latest commit: feature/assets-module
// Author : Gerrah
// Purpose : Describes a single asset — what it is, where its bytes live, and how to render it.

using Google.Cloud.Firestore;

namespace TeamSamsara.Modules.Assets.Models;

[FirestoreData]
public class AssetMetadata
{
    [FirestoreDocumentId]
    public required string Id { get; init; }

    [FirestoreProperty]
    public required AssetType type { get; init; }

    [FirestoreProperty]
    public required string FileRelativePath { get; init; }

    [FirestoreProperty]
    public required string Alt { get; init; }

    [FirestoreProperty]
    public int? Width { get; init; }

    [FirestoreProperty]
    public int? Height { get; init; }

    [FirestoreProperty]
    public string? PosterRelativePath { get; init; }

    [FirestoreProperty]
    public required string ContentType { get; init; }
}
