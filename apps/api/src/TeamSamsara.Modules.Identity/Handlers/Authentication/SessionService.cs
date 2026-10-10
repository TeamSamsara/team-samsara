// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/Authentication/SessionService.cs
// Version : 1.0.0
// Latest commit: feat/logout
// Author : Gerrah
// Purpose : Ends a verified member's sign-in on this device, or on every device.

using TeamSamsara.Modules.Identity.Repositories;

namespace TeamSamsara.Modules.Identity.Handlers;

public class SessionService : ISessionService
{
    #region Fields

    private readonly ISignInTracker _signIns;
    private readonly IAccountGateway _accounts;

    #endregion

    #region Constructors

    // Initializes the service with the sign-in tracker and the account gateway.
    public SessionService(ISignInTracker signIns, IAccountGateway accounts)
    {
        _signIns = signIns;
        _accounts = accounts;
    }

    #endregion

    #region Public Methods

    // Forgets this sign-in only; refresh tokens are shared across devices, so they stay valid.
    public async Task SignOutAsync(
        string userId,
        long authTime,
        CancellationToken cancellationToken = default)
    {
        await _signIns.RemoveVerifiedAsync(userId, authTime);
    }

    // Revokes the refresh tokens first, so no device can mint a new ID token afterwards.
    public async Task SignOutEverywhereAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        await _accounts.RevokeSessionsAsync(userId);
        await _signIns.ClearVerifiedAsync(userId);
    }

    #endregion
}
