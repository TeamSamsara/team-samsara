// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Assets/AssetsPipelineSettings.cs
// Version : 1.0.0
// Latest commit: feature/assets-module
// Author : Gerrah
// Purpose : Configuration for the Assets module's upload/delete pipeline

using System.ComponentModel.DataAnnotations;
using TeamSamsara.Shared.Http;

namespace TeamSamsara.Modules.Assets;

public class AssetsPipelineSettings : IApiKeySettings
{
    #region Fields
    public const string SectionName = "AssetsPipeline";
    #endregion

    #region Properties

    // Secret required on the upload and delete endpoints
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    #endregion
}
