// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/Passwords/PasswordSettings.cs
// Version : 1.1.0
// Latest commit: feat/password-reset-token-store
// Author : Gerrah
// Purpose : The rules a new password must meet (length only: no required digits or symbols) and
// how long a password reset token stays valid.

using System.ComponentModel.DataAnnotations;

namespace TeamSamsara.Modules.Identity.Models;

public class PasswordSettings
{
    #region Fields

    public const string SectionName = "Identity:Password";

    #endregion

    #region Properties

    // Shortest accepted password (Firebase itself refuses anything under 6)
    [Range(6, 128)]
    public int MinLength { get; set; } = 8;

    // Longest accepted password
    [Range(8, 1024)]
    public int MaxLength { get; set; } = 128;

    // How long the member has to choose a new password after confirming the reset code
    [Range(1, 60)]
    public int ResetTokenMinutes { get; set; } = 10;

    #endregion
}
