// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/Profile.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : The display/personalization record for a member, editable from settings or
// onboarding. Images are held as Assets-module asset ids; Identity never stores image data.

using Google.Cloud.Firestore;

namespace TeamSamsara.Modules.Identity.Models;

[FirestoreData]
public class Profile
{
    // The Firebase uid, which is also the Firestore document id
    [FirestoreDocumentId]
    public required string Id { get; init; }

    [FirestoreProperty]
    public required string DisplayName { get; set; }

    [FirestoreProperty]
    public string? Bio { get; set; }

    // Asset id of the profile picture in the Assets module, or null if none is set
    [FirestoreProperty]
    public string? ProfilePictureAssetId { get; set; }

    // Asset id of the banner image in the Assets module, or null if none is set
    [FirestoreProperty]
    public string? BannerAssetId { get; set; }
}
