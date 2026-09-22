// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Storage/FirebaseStorageService.cs
// Version : 1.0.0
// Latest commit: feature/shared-core-primitives
// Author : Gerrah

// Purpose : Provides the Firebase Storage implementation of IAssetStorageService.

using System;
using System.IO;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;

namespace TeamSamsara.Shared.Storage;

public class FirebaseStorageService : IAssetStorageService
{
    #region Fields

    private readonly StorageClient _storageClient;
    private readonly string _bucketName;

    #endregion

    #region Constructors

    // Initializes the service with its storage client and bucket.
    public FirebaseStorageService(StorageClient storageClient, string bucketName)
    {
        _storageClient = storageClient;
        _bucketName = bucketName;
    }

    #endregion

    #region Public Methods

    // Uploads a file under the users/ storage prefix.
    public async Task<string> UploadAsync(
        string relativePath,
        Stream content,
        string contentType)
    {
        var objectName = $"users/{relativePath}";

        await _storageClient.UploadObjectAsync(
            _bucketName,
            objectName,
            contentType,
            content);

        return objectName;
    }

    // Deletes a file under the users/ storage prefix.
    public async Task DeleteAsync(string relativePath)
    {
        var objectName = $"users/{relativePath}";

        await _storageClient.DeleteObjectAsync(_bucketName, objectName);
    }

    // Generates a temporary signed URL for a stored file.
    // TODO: Implement when time-limited private file access is required.
    public Task<string> GetSignedUrlAsync(string relativePath, TimeSpan expiry)
    {
        throw new NotImplementedException(
            "Signed URL generation requires a service account with signing " +
            "credentials — deferred until a real use case needs it.");
    }

    #endregion
}
