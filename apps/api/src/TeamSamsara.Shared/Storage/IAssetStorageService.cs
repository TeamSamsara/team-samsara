// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Storage/IAssetStorageService.cs
// Version : 1.0.0
// Latest commit: feature/shared-core-primitives
// Author : Gerrah
// Purpose : Provides the shared contract for C# API file uploads.

using System;
using System.IO;
using System.Threading.Tasks;

namespace TeamSamsara.Shared.Storage;

public interface IAssetStorageService
{
    #region Public Methods

    // Upload a file and return its storage path
    Task<string> UploadAsync(string relativePath, Stream content, string contentType);

    // Delete a file by its storage path
    Task DeleteAsync(string relativePath);

    // Generate a temporary signed URL for a file
    Task<string> GetSignedUrlAsync(string relativePath, TimeSpan expiry);

    #endregion
}
