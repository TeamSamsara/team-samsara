// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/EmailChange/IEmailChangeDelivery.cs
// Version : 1.0.0
// Latest commit: feat/email-change-service
// Author : Gerrah
// Purpose : Background work behind an email change request: issues and sends the two codes.

namespace TeamSamsara.Modules.Identity.Handlers;

public interface IEmailChangeDelivery
{
    #region Public Methods

    // Emails a code to the current address, and one to the new address unless it is unavailable.
    // An unavailable address (taken, or the member's own) gets a stored decoy code instead.
    public Task DeliverAsync(
        string userId,
        string newEmail,
        CancellationToken cancellationToken = default);

    #endregion
}
