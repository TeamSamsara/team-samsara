// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Storage/StorageServiceCollectionExtensions.cs
// Version : 2.1.0
// Latest commit: feature/asset-storage-routing
// Author : Gerrah
// Purpose : Registers both Firebase- and Postgres-backed IStorageService implementations as
// keyed services. Which one is actually used per asset type is decided by the Assets module,
// not here - this just makes both available.

using System;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using TeamSamsara.Shared.Authentication;
using TeamSamsara.Shared.Configuration;

namespace TeamSamsara.Shared.Storage;

public static class StorageServiceCollectionExtensions
{
    #region Public Methods

    // Registers both storage backends, keyed by StorageProvider. Each registration is lazy -
    // nothing runs (no credential lookup, no database connection) until something actually
    // resolves that specific keyed service, so an environment that never routes any asset
    // type to a given provider never needs that provider's credentials present.
    public static IServiceCollection AddAssetStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddValidatedOptions<StorageSettings>(configuration, StorageSettings.SectionName);

        services.AddFirebaseStorage();
        services.AddPostgresFileStorage(configuration);

        return services;
    }

    #endregion

    #region Private Methods

    // Registers the Firebase-backed IStorageService under the Firebase provider key
    private static IServiceCollection AddFirebaseStorage(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
            StorageClient.Create(GoogleCredentialProvider.TryGetFromEnvironment()));

        services.AddKeyedSingleton<IStorageService>(StorageProvider.Firebase, (sp, _) =>
        {
            var storageClient = sp.GetRequiredService<StorageClient>();
            var settings = sp.GetRequiredService<IOptions<StorageSettings>>().Value;

            if (string.IsNullOrWhiteSpace(settings.BucketName))
            {
                throw new InvalidOperationException(
                    "Storage:BucketName must be set to use Firebase-backed storage.");
            }

            return new FirebaseStorageService(storageClient, settings.BucketName);
        });

        return services;
    }

    // Registers the Postgres-backed IStorageService under the Postgres provider key
    private static IServiceCollection AddPostgresFileStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(StorageConnectionStrings.AssetsStorage);

        services.AddSingleton(sp => NpgsqlDataSource.Create(connectionString));

        services.AddKeyedSingleton<IStorageService>(StorageProvider.Postgres, (sp, _) =>
        {
            var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
            var service = new PostgresFileStorageService(dataSource);
            service.EnsureSchemaCreatedAsync().GetAwaiter().GetResult();
            return service;
        });

        return services;
    }

    #endregion
}
