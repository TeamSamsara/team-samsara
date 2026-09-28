// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Storage/StorageServiceCollectionExtensions.cs
// Version : 2.0.0
// Latest commit: feature/assets-module
// Author : Gerrah
// Purpose : Registers IStorageService, selecting the concrete implementation via Storage:Provider.

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

    // Registers IStorageService using whichever provider Storage:Provider selects
    public static IServiceCollection AddAssetStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddValidatedOptions<StorageSettings>(configuration, StorageSettings.SectionName);

        var provider = configuration
            .GetSection(StorageSettings.SectionName)
            .GetValue<StorageProvider>(nameof(StorageSettings.Provider));

        return provider switch
        {
            StorageProvider.Firebase => services.AddFirebaseStorage(),
            StorageProvider.Postgres => services.AddPostgresFileStorage(configuration),
            _ => throw new NotSupportedException($"Unsupported storage provider '{provider}'."),
        };
    }

    #endregion

    #region Private Methods

    // Registers IStorageService backed by Firebase Storage
    private static IServiceCollection AddFirebaseStorage(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
            StorageClient.Create(GoogleCredentialProvider.TryGetFromEnvironment()));

        services.AddSingleton<IStorageService>(sp =>
        {
            var storageClient = sp.GetRequiredService<StorageClient>();
            var settings = sp.GetRequiredService<IOptions<StorageSettings>>().Value;

            if (string.IsNullOrWhiteSpace(settings.BucketName))
            {
                throw new InvalidOperationException(
                    "Storage:BucketName must be set when Storage:Provider is Firebase.");
            }

            return new FirebaseStorageService(storageClient, settings.BucketName);
        });

        return services;
    }

    // Registers IStorageService backed by Postgres
    private static IServiceCollection AddPostgresFileStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(StorageConnectionStrings.AssetsStorage);

        services.AddSingleton(sp => NpgsqlDataSource.Create(connectionString));

        services.AddSingleton<IStorageService>(sp =>
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
