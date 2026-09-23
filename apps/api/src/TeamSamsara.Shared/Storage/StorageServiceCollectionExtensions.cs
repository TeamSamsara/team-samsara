// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Storage/StorageServiceCollectionExtensions.cs
// Version : 1.0.1
// Latest commit: feature/api-host
// Author : Gerrah
// Purpose : Registers StorageClient and IAssetStorageService, using GoogleCredentialProvider for credentials.

using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TeamSamsara.Shared.Authentication;
using TeamSamsara.Shared.Configuration;

namespace TeamSamsara.Shared.Storage;

public static class StorageServiceCollectionExtensions
{
    #region Public Methods

    public static IServiceCollection AddFirebaseStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddValidatedOptions<StorageSettings>(configuration, "Storage");

        services.AddSingleton(sp =>
            StorageClient.Create(GoogleCredentialProvider.TryGetFromEnvironment()));

        services.AddSingleton<IAssetStorageService>(sp =>
        {
            var storageClient = sp.GetRequiredService<StorageClient>();
            var settings = sp.GetRequiredService<IOptions<StorageSettings>>().Value;
            return new FirebaseStorageService(storageClient, settings.BucketName);
        });

        return services;
    }

    #endregion
}
