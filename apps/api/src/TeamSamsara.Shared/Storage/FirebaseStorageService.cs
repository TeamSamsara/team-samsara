// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Storage/FirebaseStorageService.cs
// Version : 1.0.0
// Latest commit: feature/assets-module
// Author : Gerrah
// Purpose : Provides the Firebase Storage implementation of IStorageService.

using System;
using System.IO;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;

namespace TeamSamsara.Shared.Storage;

public class FirebaseStorageService : IStorageService
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

    // Uploads a file and returns its storage path.
    public async Task<string> UploadAsync(string relativePath, Stream content, string contentType)
    {
        await _storageClient.UploadObjectAsync(_bucketName, relativePath, contentType, content);

        return relativePath;
    }

    // Downloads a file's content by its storage path.
    public async Task<Stream> DownloadAsync(string relativePath)
    {
        var memoryStream = new MemoryStream();

        await _storageClient.DownloadObjectAsync(_bucketName, relativePath, memoryStream);

        memoryStream.Position = 0;
        return memoryStream;
    }

    // Deletes a file by its storage path.
    public async Task DeleteAsync(string relativePath)
    {
        await _storageClient.DeleteObjectAsync(_bucketName, relativePath);
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
