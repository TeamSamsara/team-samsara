// File: /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/ProfileView.cs
// Version: 1.0.0
// Latest commit: feature/identity-profile
// Author: Gerrah
//
// Purpose: A member's profile as the API shows it, without the stored record's internals.

namespace TeamSamsara.Modules.Identity.Models;

public record ProfileView(
    string DisplayName,
    string? Bio,
    string? ProfilePictureAssetId,
    string? BannerAssetId);
