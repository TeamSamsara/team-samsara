// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/EmailChange/IEmailChangeService.cs
// Version : 1.0.0
// Latest commit: feat/email-change-service
// Author : Gerrah
// Purpose : Email change for signed-in members: request two codes, then confirm both to switch address.

using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Handlers;

public interface IEmailChangeService
{
    #region Public Methods

    // Starts a change and queues a code for the current address and one for the new address.
    // The answer is identical whether or not the new address is already in use.
    public Task<EmailChangeCodeResult> RequestChangeAsync(
        string userId,
        string newEmail,
        CancellationToken cancellationToken = default
    );

    // Checks both codes and, when both are right, switches the address and ends every session.
    // A failure never says which code was wrong. A code already accepted need not be sent again.
    public Task<EmailChangeStatus> ConfirmAsync(
        string userId,
        string oldCode,
        string newCode,
        CancellationToken cancellationToken = default
    );

    #endregion
}
