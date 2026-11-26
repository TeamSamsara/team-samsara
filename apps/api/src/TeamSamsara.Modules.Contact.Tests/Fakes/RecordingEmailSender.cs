// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Contact.Tests/Fakes/RecordingEmailSender.cs
// Version : 1.0.0
// Latest commit: feat/contact-module
// Author : Gerrah
// Purpose : An email sender that records every email it is asked to send, or fails on demand, instead of delivering it.

using TeamSamsara.Shared.Email;

namespace TeamSamsara.Modules.Contact.Tests.Fakes;

public class RecordingEmailSender : IEmailSender
{
    #region Fields

    private readonly List<EmailMessage> _messages = new();

    #endregion

    #region Properties

    public IReadOnlyList<EmailMessage> Messages => _messages;

    public Exception? ExceptionToThrow { get; set; }

    #endregion

    #region Public Methods

    public Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("The contact module only sends EmailMessage emails.");
    }

    public Task SendEmailAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        _messages.Add(message);

        return Task.CompletedTask;
    }

    #endregion
}
