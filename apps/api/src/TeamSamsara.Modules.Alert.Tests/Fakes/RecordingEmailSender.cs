// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Alert.Tests/Fakes/RecordingEmailSender.cs
// Version : 1.1.0
// Latest commit: feat/email-message-attachments
// Author : Gerrah
// Purpose : An email sender that records every email it is asked to send instead of delivering it.

using TeamSamsara.Shared.Email;

namespace TeamSamsara.Modules.Alert.Tests.Fakes;

public record SentEmail(string To, string Subject, string HtmlBody, CancellationToken CancellationToken);

public class RecordingEmailSender : IEmailSender
{
    #region Fields

    private readonly List<SentEmail> _sent = new();
    private readonly List<EmailMessage> _messages = new();

    #endregion

    #region Properties

    public IReadOnlyList<SentEmail> Sent => _sent;

    public IReadOnlyList<EmailMessage> Messages => _messages;

    #endregion

    #region Public Methods

    public Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        _sent.Add(new SentEmail(to, subject, htmlBody, cancellationToken));

        return Task.CompletedTask;
    }

    public Task SendEmailAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        _messages.Add(message);

        return Task.CompletedTask;
    }

    #endregion
}
