// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/AccountDeletion/AccountPurgeSettings.cs
// Version : 1.0.0
// Latest commit: feat/account-purge
// Author : Gerrah
// Purpose : Configuration for the permanent removal of accounts whose recovery window has ended.

using System.ComponentModel.DataAnnotations;

namespace TeamSamsara.Modules.Identity.Models;

public class AccountPurgeSettings
{
    #region Fields

    public const string SectionName = "Identity:AccountPurge";

    #endregion

    #region Properties

    // How often the purge job looks for accounts to remove
    [Range(1, 168)]
    public int IntervalHours { get; set; } = 6;

    // How long one instance holds an account while purging it; if it crashes, another may retry after this
    [Range(1, 120)]
    public int LeaseMinutes { get; set; } = 10;

    #endregion
}
