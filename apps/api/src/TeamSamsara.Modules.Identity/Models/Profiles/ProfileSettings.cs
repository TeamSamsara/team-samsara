// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/Profiles/ProfileSettings.cs
// Version : 1.1.0
// Latest commit: task/api-profile-onboarding
// Author : Gerrah
// Purpose : The limits for what a member puts on their profile: text lengths,
// handle length and how often the handle may be changed.

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

    // Shortest accepted handle, after lowercasing
    [Range(1, 30)]
    public int HandleMinLength { get; set; } = 3;

    // Longest accepted handle, after lowercasing
    [Range(1, 30)]
    public int HandleMaxLength { get; set; } = 20;

    // Days a member must wait after changing their handle before changing it
    // again. Zero turns the cooldown off.
    [Range(0, 365)]
    public int HandleChangeCooldownDays { get; set; } = 30;

    #endregion
}
