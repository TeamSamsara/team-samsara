// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Assets/Repositories/AssetStorageService.cs
// Version : 2.0.0
// Latest commit: feature/asset-storage-routing
// Author : Gerrah
// Purpose : Adapts the keyed IStorageService registrations to the Assets module's own
// contract, routing each call to the backend AssetStorageRoutingSettings assigns to its type.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TeamSamsara.Modules.Assets.Models;
using TeamSamsara.Shared.Storage;

namespace TeamSamsara.Modules.Assets.Repositories;

public class AssetStorageService : IAssetStorageService
{
    #region Fields

    private readonly IServiceProvider _serviceProvider;
    private readonly AssetStorageRoutingSettings _routing;

    #endregion

    #region Constructors

    // Initializes the adapter with the routing rules and the service provider it
    // resolves each call's backend from
    public AssetStorageService(IServiceProvider serviceProvider, IOptions<AssetStorageRoutingSettings> routing)
    {
        _serviceProvider = serviceProvider;
        _routing = routing.Value;
    }

    #endregion

    #region Public Methods

    public Task<string> UploadAsync(AssetType type, string relativePath, Stream content, string contentType)
    => Resolve(type).UploadAsync(relativePath, content, contentType);

    public Task<Stream> DownloadAsync(AssetType type, string relativePath)
    => Resolve(type).DownloadAsync(relativePath);

    public Task DeleteAsync(AssetType type, string relativePath)
    => Resolve(type).DeleteAsync(relativePath);

    public Task<string> GetSignedUrlAsync(AssetType type, string relativePath, TimeSpan expiry)
    => Resolve(type).GetSignedUrlAsync(relativePath, expiry);

    public Task<string> GetPublicUrlAsync(AssetType type, string relativePath)
    => Resolve(type).GetPublicUrlAsync(relativePath);

    #endregion

    #region Private Methods

    // Resolves the IStorageService backend that AssetStorageRoutingSettings assigns to this type
    private IStorageService Resolve(AssetType type)
    {
        var provider = _routing.For(type);
        return _serviceProvider.GetRequiredKeyedService<IStorageService>(provider);
    }

    #endregion
}
