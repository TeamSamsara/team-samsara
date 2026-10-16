// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/Profiles/IProfileService.cs
// Version : 1.1.0
// Latest commit: task/api-profile-onboarding
// Author : Gerrah
// Purpose : Reads and edits a member's own profile (name, handle, bio, images,
// onboarding) and shows any member's public profile.

using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Shared.Assets;

namespace TeamSamsara.Modules.Identity.Handlers;

public interface IProfileService
{
    #region Public Methods

    // Returns the member's profile, first giving them a handle if they have none
    public Task<ProfileResult> GetAsync(string userId);

    // Replaces the member's display name and bio
    public Task<ProfileResult> UpdateAsync(string userId, UpdateProfileRequest request);

    // Stores the uploaded image as the member's picture or banner, replacing the previous one
    public Task<ProfileResult> SetImageAsync(string userId, MemberImageKind kind, Stream content);

    // Removes the member's picture or banner
    public Task<ProfileResult> ClearImageAsync(string userId, MemberImageKind kind);

    // Gives the member a handle made from their display name, if they have none
    public Task EnsureHandleAsync(string userId);

    // Tells whether the member could take this handle: Success, InvalidHandle or HandleTaken
    public Task<ProfileStatus> CheckHandleAsync(string userId, string handle);

    // Changes the member's handle, at most once per cooldown period
    public Task<ProfileResult> SetHandleAsync(string userId, SetHandleRequest request);

    // Marks the member's first-time profile step as finished or skipped
    public Task<ProfileResult> CompleteOnboardingAsync(string userId);

    // Returns the public profile of the member who holds this handle, or null if nobody does
    public Task<PublicProfileView?> GetPublicAsync(string handle);

    #endregion
}
