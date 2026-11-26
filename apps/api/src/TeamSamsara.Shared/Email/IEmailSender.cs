// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Email/IEmailSender.cs
// Version : 1.1.0
// Latest commit: feat/email-message-attachments
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

    // Sends one HTML email to one or more recipients, with an optional reply address and attachments
    public Task SendEmailAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default);

    #endregion
}
