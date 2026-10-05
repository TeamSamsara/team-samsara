// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Assets/IAssetStorageService.cs
// Version : 2.0.0
// Latest commit: feature/asset-storage-routing
// Author : Gerrah
// Purpose : Assets-module contract for moving an asset's bytes to and from storage, routed
// by asset type to the appropriate backend.

using System;
using System.IO;
using System.Threading.Tasks;
using TeamSamsara.Modules.Assets.Models;

namespace TeamSamsara.Modules.Assets;

public interface IAssetStorageService
{
    #region Public Methods

    // Uploads a file and returns its storage path
    public Task<string> UploadAsync(AssetType type, string relativePath, Stream content, string contentType);

    // Downloads a file's content by its storage path
    public Task<Stream> DownloadAsync(AssetType type, string relativePath);

    // Deletes a file by its storage path
    public Task DeleteAsync(AssetType type, string relativePath);

    // Generates a temporary signed URL for a file
    public Task<string> GetSignedUrlAsync(AssetType type, string relativePath, TimeSpan expiry);

    // Returns a permanent public URL for a file, for backends that support one
    public Task<string> GetPublicUrlAsync(AssetType type, string relativePath);

    #endregion
}
