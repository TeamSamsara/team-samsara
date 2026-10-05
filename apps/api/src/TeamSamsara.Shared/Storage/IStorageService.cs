// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Storage/IStorageService.cs
// Version : 1.1.0
// Latest commit: feature/asset-storage-routing
// Author : Gerrah
// Purpose : Generic contract for moving file bytes to and from storage infrastructure.

using System;
using System.IO;
using System.Threading.Tasks;

namespace TeamSamsara.Shared.Storage;

public interface IStorageService
{
    #region Public Methods

    // Uploads a file and returns its storage path
    public Task<string> UploadAsync(string relativePath, Stream content, string contentType);

    // Downloads a file's content by its storage path
    public Task<Stream> DownloadAsync(string relativePath);

    // Deletes a file by its storage path
    public Task DeleteAsync(string relativePath);

    // Generates a temporary signed URL for a file
    public Task<string> GetSignedUrlAsync(string relativePath, TimeSpan expiry);

    // Returns a permanent public URL for a file, for backends that support one
    public Task<string> GetPublicUrlAsync(string relativePath);

    #endregion
}
