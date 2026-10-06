// File: /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/UpdateProfileRequest.cs
// Version: 1.0.0
// Latest commit: feature/identity-profile
// Author: Gerrah
//
// Purpose: The body for editing a member's display name and bio.

namespace TeamSamsara.Modules.Identity.Models;

public record UpdateProfileRequest(string DisplayName, string? Bio);
