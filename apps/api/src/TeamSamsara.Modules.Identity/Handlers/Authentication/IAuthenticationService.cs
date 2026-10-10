// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/IAuthenticationService.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : Logging into a member account. After the client signs in with Firebase, the
// sign-in is checked here: a recognized client is verified straight away, an unrecognized one
// must confirm an emailed code first. A sign-in is identified by its Firebase auth_time.

using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Handlers;

public interface IAuthenticationService
{
    #region Public Methods

    // Checks a fresh sign-in. Verifies it if the client is recognized; otherwise sends a
    // challenge code and reports that one is required.
    public Task<LoginResult> CheckSignInAsync(
        string userId,
        long authTime,
        CancellationToken cancellationToken = default);

    // Checks the challenge code. If valid, restores the account when it was deleted, remembers
    // the client and verifies the sign-in.
    public Task<AuthenticationStatus> ConfirmChallengeAsync(
        string userId,
        long authTime,
        string code,
        CancellationToken cancellationToken = default);

    // Sends a fresh challenge code to a member whose sign-in is waiting for one
    public Task<LoginResult> ResendChallengeAsync(
        string userId,
        CancellationToken cancellationToken = default);

    #endregion
}
