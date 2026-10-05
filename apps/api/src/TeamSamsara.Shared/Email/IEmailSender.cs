// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Email/IEmailSender.cs
// Version : 1.0.0
// Latest commit: feature/alerts-module
// Author : Gerrah
// Purpose : Generic contract for delivering an email.

namespace TeamSamsara.Shared.Email;

public interface IEmailSender
{
    #region Public Methods

    // Sends one HTML email to a single recipient
    public Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);

    #endregion
}
