// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/IPasswordResetDelivery.cs
// Version : 1.0.0
// Latest commit: feat/password-reset-service
// Author : Gerrah
// Purpose : Background work behind a reset request: issues the code and sends the emails.

using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Handlers;

public interface IPasswordResetDelivery
{
    #region Public Methods

    // Emails a reset code to a member, or stores a decoy code if the address has no member account.
    public Task DeliverAsync(
        string email,
        ClientInfo client,
        CancellationToken cancellationToken = default);

    #endregion
}
