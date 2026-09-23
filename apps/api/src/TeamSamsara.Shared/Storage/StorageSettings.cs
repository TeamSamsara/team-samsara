// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Storage/StorageSettings.cs
// Version : 1.0.0
// Latest commit: feature/shared-core-primitives
// Author : Gerrah
// Purpose : Binds the shared Firebase Storage bucket name from configuration.

using System.ComponentModel.DataAnnotations;

namespace TeamSamsara.Shared.Storage;

public class StorageSettings
{
    #region Properties

    [Required]
    public string BucketName { get; set; } = string.Empty;

    #endregion
}
