// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/Authentication/ISessionService.cs
// Version : 1.0.0
// Latest commit: feat/logout
// Author : Gerrah
// Purpose : Ends a verified member's sign-in on this device, or on every device.

namespace TeamSamsara.Modules.Identity.Handlers;

public interface ISessionService
{
    #region Public Methods

    // Forgets this sign-in, so its token stops counting as Member at once; other devices stay signed in.
    public Task SignOutAsync(
        string userId,
        long authTime,
        CancellationToken cancellationToken = default
    );

    // Revokes every refresh token and forgets every verified sign-in, like a password change does.
    public Task SignOutEverywhereAsync(
        string userId,
        CancellationToken cancellationToken = default
    );

    #endregion
}
