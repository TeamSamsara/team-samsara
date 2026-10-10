// File: /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/IProfileService.cs
// Version: 1.0.0
// Latest commit: feature/identity-profile
// Author: Gerrah
//
// Purpose: Reads and edits a member's own profile: name, bio, profile picture and banner.

using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Shared.Assets;

namespace TeamSamsara.Modules.Identity.Handlers;

public interface IProfileService
{
    #region Public Methods

    // Returns the member's profile
    public Task<ProfileResult> GetAsync(string userId);

    // Replaces the member's display name and bio
    public Task<ProfileResult> UpdateAsync(string userId, UpdateProfileRequest request);

    // Stores the uploaded image as the member's picture or banner, replacing the previous one
    public Task<ProfileResult> SetImageAsync(string userId, MemberImageKind kind, Stream content);

    // Removes the member's picture or banner
    public Task<ProfileResult> ClearImageAsync(string userId, MemberImageKind kind);

    #endregion
}
