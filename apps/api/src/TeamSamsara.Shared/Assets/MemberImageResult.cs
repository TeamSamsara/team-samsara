// File: /team-samsara/apps/api/src/TeamSamsara.Shared/Assets/MemberImageResult.cs
// Version: 1.0.0
// Latest commit: feature/identity-profile
// Author: Gerrah
//
// Purpose: What storing a member image returns: the outcome, plus the new asset id on success.

namespace TeamSamsara.Shared.Assets;

public record MemberImageResult(MemberImageStatus Status, string? AssetId);
