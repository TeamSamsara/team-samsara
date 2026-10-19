// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/EmailChange/IEmailChangeService.cs
// Version : 1.1.0
// Latest commit: task/web-account-security
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

    // Checks the codes sent, one or both; a blank code means "not sent yet". Once both are accepted the
    // address switches and every session ends. A right code is remembered and answers CodeAccepted
    // while the other is missing. A wrong code is never told apart from a taken address.
    public Task<EmailChangeStatus> ConfirmAsync(
        string userId,
        string? oldCode,
        string? newCode,
        CancellationToken cancellationToken = default
    );

    #endregion
}
