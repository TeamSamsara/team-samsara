// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Assets/Repositories/AssetStorageService.cs
// Version : 1.0.0
// Latest Commit : feature/assets-modules
// Author : Gerrah
// Purpose : Adapts generic IStorageService to the Asset module own Storage context

using System;
using System.IO;
using System.Threading.Tasks;
using TeamSamsara.Shared.Storage;

namespace TeamSamsara.Modules.Assets.Repositories;

public class AssetStorageService : IAssetStorageService
{
    #region Fields

    private readonly IStorageService _storageService;

    #endregion

    #region Constructors

    // Initialize the adapter with the generic storage service it delegates
    public AssetStorageService(IStorageService storageService)
    {
        _storageService = storageService;
    }

    #endregion

    #region Public Methods

    public Task<string> UploadAsync(string relativePath, Stream content, string contentType)
    => _storageService.UploadAsync(relativePath, content, contentType);

    public Task<Stream> DownloadAsync(string relativePath)
    => _storageService.DownloadAsync(relativePath);

    public Task DeleteAsync(string relativePath)
    => _storageService.DeleteAsync(relativePath);

    public Task<string> GetSignedUrlAsync(string relativePath, TimeSpan expiry)
    => _storageService.GetSignedUrlAsync(relativePath, expiry);


    #endregion
}
