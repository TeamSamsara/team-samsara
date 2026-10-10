// File: /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/ProfileResult.cs
// Version: 1.0.0
// Latest commit: feature/identity-profile
// Author: Gerrah
//
// Purpose: What a profile operation returns: the outcome, plus the updated profile on success.

namespace TeamSamsara.Modules.Identity.Models;

public record ProfileResult(ProfileStatus Status, ProfileView? Profile);
