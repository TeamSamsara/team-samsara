// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Endpoints/IdentityStatusCodes.cs
// Version : 1.1.0
// Latest commit: feature/identity-profile
// Author : Gerrah
// Purpose : Translates the outcome of a registration, authentication or profile step into the
// HTTP status code the endpoint answers with. The outcome itself always travels in the body
// too, so the client can react to the exact reason.

using Microsoft.AspNetCore.Http;
using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Endpoints;

public static class IdentityStatusCodes
{
    #region Public Methods

    // HTTP status for a registration outcome
    public static int For(RegistrationStatus status)
    {
        return status switch
        {
            RegistrationStatus.Success => StatusCodes.Status200OK,
            RegistrationStatus.AlreadyRegistered => StatusCodes.Status409Conflict,
            RegistrationStatus.AccountNotFound => StatusCodes.Status404NotFound,
            RegistrationStatus.CooldownActive => StatusCodes.Status429TooManyRequests,
            RegistrationStatus.TooManyAttempts => StatusCodes.Status429TooManyRequests,
            RegistrationStatus.InvalidCode => StatusCodes.Status400BadRequest,
            RegistrationStatus.CodeExpired => StatusCodes.Status400BadRequest,
            RegistrationStatus.NoPendingCode => StatusCodes.Status400BadRequest,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }

    // HTTP status for an authentication outcome
    public static int For(AuthenticationStatus status)
    {
        return status switch
        {
            AuthenticationStatus.Authenticated => StatusCodes.Status200OK,
            AuthenticationStatus.ChallengeRequired => StatusCodes.Status200OK,
            AuthenticationStatus.RegistrationIncomplete => StatusCodes.Status403Forbidden,
            AuthenticationStatus.AccountNotFound => StatusCodes.Status404NotFound,
            AuthenticationStatus.CooldownActive => StatusCodes.Status429TooManyRequests,
            AuthenticationStatus.TooManyAttempts => StatusCodes.Status429TooManyRequests,
            AuthenticationStatus.InvalidCode => StatusCodes.Status400BadRequest,
            AuthenticationStatus.CodeExpired => StatusCodes.Status400BadRequest,
            AuthenticationStatus.NoPendingCode => StatusCodes.Status400BadRequest,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }

    // HTTP status for a profile outcome
    public static int For(ProfileStatus status)
    {
        return status switch
        {
            ProfileStatus.Success => StatusCodes.Status200OK,
            ProfileStatus.ProfileNotFound => StatusCodes.Status404NotFound,
            ProfileStatus.InvalidDisplayName => StatusCodes.Status400BadRequest,
            ProfileStatus.InvalidBio => StatusCodes.Status400BadRequest,
            ProfileStatus.ImageUnsupportedType => StatusCodes.Status415UnsupportedMediaType,
            ProfileStatus.ImageTooLarge => StatusCodes.Status413PayloadTooLarge,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }

    #endregion
}
