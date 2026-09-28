// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Storage/IAssetStorageService.cs
// Version : 1.0.0
// Latest commit: feature/assets-module
// Author : Gerrah
// Purpose : Provides the shared contract for C# API file storage.

using System;
using System.IO;
using System.Threading.Tasks;

namespace TeamSamsara.Shared.Storage;

public interface IAssetStorageService
{
    #region Public Methods

    // Upload a file and return its storage path
    public Task<string> UploadAsync(string relativePath, Stream content, string contentType);

    // Download a file's content by its storage path
    public Task<Stream> DownloadAsync(string relativePath);

    // Delete a file by its storage path
    public Task DeleteAsync(string relativePath);

    // Generate a temporary signed URL for a file
    public Task<string> GetSignedUrlAsync(string relativePath, TimeSpan expiry);

    #endregion
}
