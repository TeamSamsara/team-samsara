// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Email/EmailAttachment.cs
// Version : 1.0.0
// Latest commit: feat/email-message-attachments
// Author : Gerrah
// Purpose : One file attached to an outgoing email.

namespace TeamSamsara.Shared.Email;

public record EmailAttachment(string FileName, byte[] Content);
