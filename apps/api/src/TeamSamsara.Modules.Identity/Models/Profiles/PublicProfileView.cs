// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/Profiles/PublicProfileView.cs
// Version : 1.0.0
// Latest commit: task/api-profile-onboarding
// Author : Gerrah
// Purpose : A member's profile as any visitor sees it. Holds only what a public
// page shows: no member id, no onboarding state, no handle history.

namespace TeamSamsara.Modules.Identity.Models;

public record PublicProfileView(
    string DisplayName,
    string Handle,
    string? Bio,
    string? ProfilePictureAssetId,
    string? BannerAssetId);
