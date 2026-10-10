// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/PasswordSettings.cs
// Version : 1.0.0
// Latest commit: feat/password-change-service
// Author : Gerrah
// Purpose : The rules a new password must meet. Length only: no required digits or symbols.

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

    #endregion
}
