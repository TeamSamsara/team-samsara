// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Assets/AssetStorageRoutingSettings.cs
// Version : 1.0.0
// Latest commit: feature/asset-storage-routing
// Author : Gerrah
// Purpose : Binds which StorageProvider each AssetType is stored with.

using System;
using System.ComponentModel.DataAnnotations;
using TeamSamsara.Modules.Assets.Models;
using TeamSamsara.Shared.Storage;

namespace TeamSamsara.Modules.Assets;

public class AssetStorageRoutingSettings
{
    #region Fields

    public const string SectionName = "AssetStorageRouting";

    #endregion

    #region Properties

    [Required]
    public StorageProvider Image { get; set; }

    [Required]
    public StorageProvider Video { get; set; }

    [Required]
    public StorageProvider File { get; set; }

    #endregion

    #region Public Methods

    // Returns which provider a given asset type should be stored with
    public StorageProvider For(AssetType type) => type switch
    {
        AssetType.Image => Image,
        AssetType.Video => Video,
        AssetType.File => File,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown asset type."),
    };

    #endregion
}
