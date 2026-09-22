// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Persistence/FirestoreSettings.cs
// Version : 1.0.0
// Latest commit: feature/shared-core-primitives
// Author : Gerrah

// Purpose : Defines the configuration required to connect to Firestore.

using System.ComponentModel.DataAnnotations;

namespace TeamSamsara.Shared.Persistence;

public class FirestoreSettings
{
    #region Properties

    [Required]
    public string ProjectId { get; set; } = string.Empty;

    #endregion
}
