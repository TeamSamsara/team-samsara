// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Repositories/IHandleStore.cs
// Version : 1.0.0
// Latest commit: task/api-profile-onboarding
// Author : Gerrah
// Purpose : Keeps member @handles unique, independent of where they are stored.

using System.Reflection.Metadata;
using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Repositories;

public interface IHandleStore
{
    #region Public Methods


    // Return the uid of the member who hold the handle, or null if nobody does
    public Task<string?> GetOwnerAsync(string handle);

    // Gives the handle to the member and saves it on their profile in one step
    // Frees up their previous handle, changedAt, when given, is saved as the change time.
    public Task<HandleClaimStatus> ClaimAsync(string userId, string handle, DateTimeOffset? changedAt);

    // Free the handle the member holds, if any
    public Task ReleaseAsync(string userId);

    #endregion
}
