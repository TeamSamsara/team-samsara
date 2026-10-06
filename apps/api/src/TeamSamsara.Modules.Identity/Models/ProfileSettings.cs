// File: /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/ProfileSettings.cs
// Version: 1.0.0
// Latest commit: feature/identity-profile
// Author: Gerrah
//
// Purpose: The length limits for the text a member puts on their profile.

using System.ComponentModel.DataAnnotations;

namespace TeamSamsara.Modules.Identity.Models;

public class ProfileSettings
{
    #region Fields

    public const string SectionName = "Identity:Profile";

    #endregion

    #region Properties

    // Shortest accepted display name, after trimming
    [Range(1, 50)]
    public int DisplayNameMinLength { get; set; } = 1;

    // Longest accepted display name, after trimming
    [Range(1, 100)]
    public int DisplayNameMaxLength { get; set; } = 50;

    // Longest accepted bio, after trimming
    [Range(1, 2000)]
    public int BioMaxLength { get; set; } = 300;

    #endregion
}
