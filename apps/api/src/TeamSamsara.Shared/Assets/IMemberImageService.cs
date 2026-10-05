// File: /team-samsara/apps/api/src/TeamSamsara.Shared/Assets/IMemberImageService.cs
// Version: 1.0.0
// Latest commit: feature/identity-profile
// Author: Gerrah
//
// Purpose: Contract any module uses to store and remove member images, implemented by Assets.

namespace TeamSamsara.Shared.Assets;

public interface IMemberImageService
{
    #region Public Methods

    // Validates the image and stores it, returning the new asset id on success
    public Task<MemberImageResult> StoreAsync(MemberImageKind kind, Stream content);

    // Deletes a stored member image; does nothing if it no longer exists
    public Task DeleteAsync(string assetId);

    #endregion
}
