// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Assets/Services/AssetService.cs
// Version : 2.1.0
// Latest commit: feat/account-purge
// Author : Gerrah
// Purpose : Orchestrates asset upload, retrieval, and deletion across file storage and metadata.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TeamSamsara.Modules.Assets.Models;
using TeamSamsara.Modules.Assets.Repositories;
using TeamSamsara.Shared.Http;
using TeamSamsara.Shared.Results;

namespace TeamSamsara.Modules.Assets.Services;

public class AssetService
{
    #region Fields

    private readonly IAssetStorageService _storageService;
    private readonly IAssetMetadataStore _metadataStore;
    private readonly ILogger<AssetService> _logger;

    #endregion

    #region Constructors

    // Initializes the service with the storage and metadata dependencies it orchestrates
    public AssetService(
        IAssetStorageService storageService,
        IAssetMetadataStore metadataStore,
        ILogger<AssetService> logger)
    {
        _storageService = storageService;
        _metadataStore = metadataStore;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    // Uploads a file's bytes and creates its metadata record, inferring type and dimensions.
    // If metadata creation fails, the already-uploaded file is deleted before failing.
    public async Task<AssetMetadata> UploadAssetAsync(Stream content, string fileName, string contentType, string alt)
    {
        var bytes = await ReadAllBytesAsync(content);
        var (assetType, width, height) = InspectContent(bytes, contentType);

        var id = Guid.NewGuid().ToString();
        var relativePath = BuildRelativePath(id, fileName);

        await UploadBytesAsync(assetType, relativePath, bytes, contentType);

        var metadata = BuildMetadata(id, assetType, relativePath, contentType, alt, width, height);

        await CreateMetadataOrCleanupAsync(metadata, relativePath);

        return metadata;
    }

    // Retrieves an asset's metadata by id, or null if it doesn't exist
    public Task<AssetMetadata?> GetAssetAsync(string id) => _metadataStore.GetByIdAsync(id);

    // Retrieves all assets metadata, optionally filtered type
    public Task<IReadOnlyList<AssetMetadata>> ListAssetsAsync(AssetType? type) => _metadataStore.ListAsync(type);

    // Retrieves how an asset's file should be served - proxied or redirected - or null
    // if it doesn't exist. Video is served by redirecting to its storage backend's own
    // public URL; everything else is proxied through the API as a stream.
    public async Task<AssetFileResult?> GetAssetFileAsync(string id)
    {
        var metadata = await _metadataStore.GetByIdAsync(id);

        if (metadata is null)
        {
            return null;
        }

        if (metadata.type == AssetType.Video)
        {
            var url = await _storageService.GetPublicUrlAsync(metadata.type, metadata.FileRelativePath);
            return AssetFileResult.Redirect(url);
        }

        var content = await _storageService.DownloadAsync(metadata.type, metadata.FileRelativePath);
        return AssetFileResult.Proxied(content, metadata.ContentType);
    }

    // Deletes the stored file, then the metadata, so a failure leaves the asset findable and retryable; false if it doesn't exist
    public async Task<bool> DeleteAssetAsync(string id)
    {
        var metadata = await _metadataStore.GetByIdAsync(id);

        if (metadata is null)
        {
            return false;
        }

        await DeleteFileAsync(metadata);
        await DeleteMetadataAsync(id);

        return true;
    }

    #endregion

    #region Private Methods

    // Reads a stream fully into memory once, so it can be inspected and then uploaded
    // without needing to be read twice.
    private static async Task<byte[]> ReadAllBytesAsync(Stream content)
    {
        using var memoryStream = new MemoryStream();
        await content.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    // Determines the asset's type from its content type, and its dimensions if it's an image
    private static (AssetType Type, int? Width, int? Height) InspectContent(byte[] bytes, string contentType)
    {
        var assetType = InferAssetType(contentType);

        if (assetType != AssetType.Image)
        {
            return (assetType, null, null);
        }

        var (width, height) = ImageDimensionReader.TryRead(bytes);
        return (assetType, width, height);
    }

    // Infers the asset's type from its content type
    private static AssetType InferAssetType(string contentType)
    {
        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return AssetType.Image;
        }

        if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
        {
            return AssetType.Video;
        }

        return AssetType.File;
    }

    // Builds the storage path for a new asset from its id and original filename
    private static string BuildRelativePath(string id, string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return $"{id}{extension}";
    }

    // Uploads the asset's bytes to storage
    private async Task UploadBytesAsync(AssetType type, string relativePath, byte[] bytes, string contentType)
    {
        using var uploadStream = new MemoryStream(bytes);
        await _storageService.UploadAsync(type, relativePath, uploadStream, contentType);
    }

    // Assembles the metadata record for a newly-uploaded asset
    private static AssetMetadata BuildMetadata(
        string id,
        AssetType assetType,
        string relativePath,
        string contentType,
        string alt,
        int? width,
        int? height)
        => new()
        {
            Id = id,
            type = assetType,
            FileRelativePath = relativePath,
            Alt = alt,
            Width = width,
            Height = height,
            ContentType = contentType,
        };

    // Creates the metadata record, deleting the already-uploaded file and failing if it can't
    private async Task CreateMetadataOrCleanupAsync(AssetMetadata metadata, string relativePath)
    {
        try
        {
            await _metadataStore.CreateAsync(metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create metadata for asset '{AssetId}'.", metadata.Id);

            await TryDeleteFileAsync(metadata.type, relativePath, metadata.Id);

            throw new AppException(Error.Failure(ErrorCodes.AssetMetadataCreationFailed, ResultMessages.AssetMetadataCreationFailed));
        }
    }

    // Deletes the asset's stored file, failing the whole delete (metadata untouched) if storage refuses
    private async Task DeleteFileAsync(AssetMetadata metadata)
    {
        try
        {
            await _storageService.DeleteAsync(metadata.type, metadata.FileRelativePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete stored file '{RelativePath}' for asset '{AssetId}'.",
                metadata.FileRelativePath, metadata.Id);
            throw new AppException(Error.Failure(ErrorCodes.AssetFileDeletionFailed, ResultMessages.AssetFileDeletionFailed));
        }
    }

    // Deletes the asset's metadata record, failing the delete if the store refuses
    private async Task DeleteMetadataAsync(string id)
    {
        try
        {
            await _metadataStore.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete metadata for asset '{AssetId}'.", id);
            throw new AppException(Error.Failure(ErrorCodes.AssetMetadataDeletionFailed, ResultMessages.AssetMetadataDeletionFailed));
        }
    }

    // Best-effort delete of a stored file, used only to clean up after a failed metadata write
    private async Task TryDeleteFileAsync(AssetType type, string relativePath, string assetId)
    {
        try
        {
            await _storageService.DeleteAsync(type, relativePath);
        }
        catch (Exception cleanupEx)
        {
            _logger.LogError(cleanupEx,
                "Failed to delete stored file '{RelativePath}' for asset '{AssetId}'.",
                relativePath, assetId);
        }
    }

    #endregion
}
