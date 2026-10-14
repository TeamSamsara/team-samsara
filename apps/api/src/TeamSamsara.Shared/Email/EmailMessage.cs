// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Email/EmailMessage.cs
// Version : 1.0.0
// Latest commit: feat/email-message-attachments
// Author : Gerrah
// Purpose : One outgoing HTML email with its recipients, optional reply address and attachments.

namespace TeamSamsara.Shared.Email;

public record EmailMessage
{
    #region Properties

    public required IReadOnlyList<string> To { get; init; }

    public required string Subject { get; init; }

    public required string HtmlBody { get; init; }

    public string? ReplyTo { get; init; }

    public IReadOnlyList<EmailAttachment> Attachments { get; init; } = Array.Empty<EmailAttachment>();

    #endregion
}
