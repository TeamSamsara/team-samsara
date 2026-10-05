// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Email/EmailSettings.cs
// Version : 1.0.0
// Latest commit: feature/alerts-module
// Author : Gerrah
// Purpose : Binds the email channel's configuration (Resend API key and sender identity).

using System.ComponentModel.DataAnnotations;

namespace TeamSamsara.Shared.Email;

public class EmailSettings
{
    #region Fields

    public const string SectionName = "Email";

    #endregion

    #region Properties

    // Resend API key, sent as the Authorization: Bearer value
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    // The verified Resend sender identity, e.g. "Team Samsara <alerts@teamsamsara.app>"
    [Required]
    public string FromAddress { get; set; } = string.Empty;

    #endregion
}
