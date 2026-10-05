// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Repositories/FirebaseAccountGateway.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : Firebase Admin implementation of the account gateway.

using FirebaseAdmin.Auth;
using TeamSamsara.Shared.Authentication;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Repositories;

public class FirebaseAccountGateway : IAccountGateway
{
    #region Public Methods

    // Sets the access level claim. Firebase replaces the whole custom-claims set on each call,
    // which is safe today because accessLevel is the only custom claim in use.
    public async Task SetAccessLevelAsync(string uid, AccessLevel accessLevel)
    {
        var claims = new Dictionary<string, object>
        {
            [ClaimNames.AccessLevel] = accessLevel.ToString()
        };

        await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(uid, claims);
    }

    // Retrieves the account's email address, or null if the account does not exist
    public async Task<string?> GetEmailAsync(string uid)
    {
        try
        {
            var user = await FirebaseAuth.DefaultInstance.GetUserAsync(uid);
            return user.Email;
        }
        catch (FirebaseAuthException exception)
            when (exception.AuthErrorCode == AuthErrorCode.UserNotFound)
        {
            return null;
        }
    }

    // Deletes the authentication account, treating "already gone" as success
    public async Task DeleteAsync(string uid)
    {
        try
        {
            await FirebaseAuth.DefaultInstance.DeleteUserAsync(uid);
        }
        catch (FirebaseAuthException exception)
            when (exception.AuthErrorCode == AuthErrorCode.UserNotFound)
        {
            // Already deleted - nothing to do.
        }
    }

    #endregion
}
