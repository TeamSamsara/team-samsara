// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Repositories/IAccountGateway.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : The server-side operations Identity needs on the authentication provider's account
// record (credentials, email, token claims). Sign-in itself happens client-side; this covers
// only what the API must do on its own.

using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Repositories;

public interface IAccountGateway
{
    #region Public Methods

    // Sets the access level claim carried on the account's ID tokens. Takes effect on the
    // client's next token refresh.
    public Task SetAccessLevelAsync(string uid, AccessLevel accessLevel);

    // Retrieves the account's email address, or null if the account does not exist
    public Task<string?> GetEmailAsync(string uid);

    // Deletes the authentication account. Succeeds silently if it is already gone, so a purge
    // that failed halfway can safely be retried.
    public Task DeleteAsync(string uid);

    #endregion
}
