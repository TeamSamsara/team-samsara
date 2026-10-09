// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Repositories/FirebaseAccountGateway.cs
// Version : 1.1.0
// Latest commit: feat/account-gateway-password
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

    // Finds the account registered with this email address, or null if there is none
    public async Task<string?> GetUserIdByEmailAsync(string email)
    {
        try
        {
            var user = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(email);
            return user.Uid;
        }
        catch (FirebaseAuthException exception)
            when (exception.AuthErrorCode == AuthErrorCode.UserNotFound)
        {
            return null;
        }
    }

    // Replaces the account's password
    public async Task SetPasswordAsync(string uid, string newPassword)
    {
        var changes = new UserRecordArgs
        {
            Uid = uid,
            Password = newPassword
        };

        await FirebaseAuth.DefaultInstance.UpdateUserAsync(changes);
    }

    // Invalidates every refresh token the account has, treating "already gone" as success
    public async Task RevokeSessionsAsync(string uid)
    {
        try
        {
            await FirebaseAuth.DefaultInstance.RevokeRefreshTokensAsync(uid);
        }
        catch (FirebaseAuthException exception)
            when (exception.AuthErrorCode == AuthErrorCode.UserNotFound)
        {
            // The account is gone, so there is nothing left to sign out.
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
