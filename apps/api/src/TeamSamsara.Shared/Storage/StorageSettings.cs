// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Storage/StorageSettings.cs
// Version : 1.0.2
// Latest commit: feature/asset-storage-routing
// Author : Gerrah
// Purpose : Binds the shared Firebase Storage bucket name from configuration.

namespace TeamSamsara.Shared.Storage;

public class StorageSettings
{
    #region Fields

    public const string SectionName = "Storage";

    #endregion

    #region Properties

    public string BucketName { get; set; } = string.Empty;

    #endregion
}
