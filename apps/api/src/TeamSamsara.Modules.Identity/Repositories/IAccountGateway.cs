// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Repositories/IAccountGateway.cs
// Version : 1.1.0
// Latest commit: feat/account-gateway-password
// Author : Gerrah
// Purpose : The server-side operations Identity needs on the authentication provider's account
// record (credentials, email, token claims). Sign-in itself happens client-side; this covers
// only what the API must do on its own.

using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Repositories;

public interface IAccountGateway
{
    #region Public Methods

    // Sets the access level claim carried on the account's ID token.
    // Takes effect on the client's next token refresh.
    public Task SetAccessLevelAsync(string uid, AccessLevel accessLevel);

    // Retrieves the account's email address.
    // Returns null if the account doesn't exist.
    public Task<string?> GetEmailAsync(string uid);

    // Finds the account registered with this email address.
    // Returns null if no account exists with that email.
    public Task<string?> GetUserIdByEmailAsync(string email);

    // Replaces the account's password.
    // Throws if the account doesn't exist.
    public Task SetPasswordAsync(string uid, string newPassword);

    // Invalidates every refresh token of the account.
    // Succeeds silently if the account doesn't exist.
    public Task RevokeSessionsAsync(string uid);

    // Deletes the authentication account.
    // Succeeds silently if the account doesn't exist.
    public Task DeleteAsync(string uid);

    #endregion
}
