// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Repositories/IAccountGateway.cs
// Version : 1.2.0
// Latest commit: feat/email-change-foundation
// Author : Gerrah
// Purpose : The server-side operations Identity needs on the authentication provider's account record.

using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Repositories;

public interface IAccountGateway
{
    #region Public Methods

    // Sets the access level claim on the account's ID token, effective at the client's next refresh
    public Task SetAccessLevelAsync(string uid, AccessLevel accessLevel);

    // Returns the account's email, or null if the account doesn't exist
    public Task<string?> GetEmailAsync(string uid);

    // Returns the id of the account registered with this email, or null if there is none
    public Task<string?> GetUserIdByEmailAsync(string email);

    // Replaces the account's password; throws if the account doesn't exist
    public Task SetPasswordAsync(string uid, string newPassword);

    // Replaces the account's email and marks it verified; throws if the account doesn't exist
    // or the address already belongs to another account
    public Task SetEmailAsync(string uid, string newEmail);

    // Invalidates every refresh token of the account; succeeds silently if it doesn't exist
    public Task RevokeSessionsAsync(string uid);

    // Deletes the authentication account; succeeds silently if it doesn't exist
    public Task DeleteAsync(string uid);

    #endregion
}
