// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Persistence/FirestoreSettings.cs
// Version : 1.0.1
// Latest commit: feature/string-magic-value-conventions
// Author : Gerrah

// Purpose : Defines the configuration required to connect to Firestore.

using System.ComponentModel.DataAnnotations;

namespace TeamSamsara.Shared.Persistence;

public class FirestoreSettings
{
    #region Fields

    public const string SectionName = "Firestore";

    #endregion

    #region Properties

    [Required]
    public string ProjectId { get; set; } = string.Empty;

    #endregion
}
