// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/AuthenticationSettings.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : Configuration for login and sign-in verification tracking.

using System.ComponentModel.DataAnnotations;

namespace TeamSamsara.Modules.Identity.Models;

public class AuthenticationSettings
{
    #region Fields

    public const string SectionName = "Identity:Authentication";

    #endregion

    #region Properties

    // How many verified sign-ins are remembered per member (oldest are dropped first)
    [Range(1, 100)]
    public int MaxVerifiedSignIns { get; set; } = 20;

    // How long a member's verified sign-ins are cached, so not every request reads Firestore.
    // This is also the longest a revoked sign-in could keep working.
    [Range(1, 300)]
    public int VerifiedCacheSeconds { get; set; } = 30;

    // Days after deletion during which logging back in restores the account
    [Range(1, 365)]
    public int RecoveryWindowDays { get; set; } = 30;

    #endregion
}
