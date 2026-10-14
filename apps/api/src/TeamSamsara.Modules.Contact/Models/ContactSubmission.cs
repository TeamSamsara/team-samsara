// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Contact/Models/ContactSubmission.cs
// Version : 1.0.0
// Latest commit: feat/contact-module
// Author : Gerrah
// Purpose : A contact form submission exactly as the visitor sent it, before any validation.

namespace TeamSamsara.Modules.Contact.Models;

public record ContactSubmission
{
    #region Properties

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public string? Category { get; init; }

    public string? Message { get; init; }

    public string? Honeypot { get; init; }

    public IReadOnlyList<ContactFile> Files { get; init; } = Array.Empty<ContactFile>();

    #endregion
}
