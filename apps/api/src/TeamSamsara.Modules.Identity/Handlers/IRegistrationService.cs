// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/IRegistrationService.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : Turns a newly created account into a member: sends the verification code, then on
// confirmation grants the Member level and creates the member's profile.

using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Handlers;

public interface IRegistrationService
{
    #region Public Methods

    // Begins registration for an account: records it as a Guest and sends the first code
    public Task<RegistrationStartResult> StartAsync(
        string userId,
        CancellationToken cancellationToken = default);

    // Sends a fresh code to an account whose registration has begun but is not complete
    public Task<RegistrationStartResult> ResendAsync(
        string userId,
        CancellationToken cancellationToken = default);

    // Checks the submitted code and, if valid, completes registration
    public Task<RegistrationStatus> ConfirmAsync(
        string userId,
        string code,
        CancellationToken cancellationToken = default);

    #endregion
}
