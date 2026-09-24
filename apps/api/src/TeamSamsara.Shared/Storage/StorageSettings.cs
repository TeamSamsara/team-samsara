// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Storage/StorageSettings.cs
// Version : 1.0.1
// Latest commit: feature/string-magic-value-conventions
// Author : Gerrah
// Purpose : Binds the shared Firebase Storage bucket name from configuration.

using System.ComponentModel.DataAnnotations;

namespace TeamSamsara.Shared.Storage;

public class StorageSettings
{
    #region Fields

    public const string SectionName = "Storage";

    #endregion

    #region Properties

    [Required]
    public string BucketName { get; set; } = string.Empty;

    #endregion
}
