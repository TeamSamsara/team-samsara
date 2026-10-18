// File: /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/ProfileView.cs
// Version: 1.1.0
// Latest commit: task/web-account-settings
// Author: Gerrah
//
// Purpose: A member's profile as the API shows it, without the stored record's internals.
// HandleChangeableAt is the moment the handle can be changed again, or null when it can be
// changed now.

namespace TeamSamsara.Modules.Identity.Models;

public record ProfileView(
    string DisplayName,
    string? Handle,
    string? Bio,
    string? ProfilePictureAssetId,
    string? BannerAssetId,
    bool OnboardingCompleted,
    DateTimeOffset? HandleChangeableAt);
