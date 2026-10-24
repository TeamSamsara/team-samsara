// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Repositories/IHandleStore.cs
// Version : 1.1.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Keeps member @handles unique, independent of where they are stored.

using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Repositories;

public interface IHandleStore
{
    #region Public Methods

    // Return the uid of the member who holds the handle, or null if nobody does
    public Task<string?> GetOwnerAsync(string handle);

    // Gives the handle to the member and saves it on their profile in one step, freeing up
    // their previous handle. With a change, the member's own request, the cooldown is
    // enforced and, once onboarding is done, the change time is saved. Without one, the
    // handle is assigned by the system and neither applies.
    public Task<HandleClaimResult> ClaimAsync(string userId, string handle, HandleChange? change);

    // Free the handle the member holds, if any
    public Task ReleaseAsync(string userId);

    #endregion
}
