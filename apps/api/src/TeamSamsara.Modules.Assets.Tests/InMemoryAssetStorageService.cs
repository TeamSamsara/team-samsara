// File: /team-samsara/apps/api/src/TeamSamsara.Modules.Assets.Tests/Fakes/InMemoryAssetStorageService.cs
// Version: 1.0.0
// Latest commit: feature/identity-profile
// Author: Gerrah
//
// Purpose: An in-memory asset file storage that remembers what was uploaded.

using TeamSamsara.Modules.Assets.Models;

namespace TeamSamsara.Modules.Assets.Tests.Fakes;

public class InMemoryAssetStorageService : IAssetStorageService
{
    #region Fields

    private readonly Dictionary<string, StoredFile> _files = new();

    #endregion

    #region Properties

    // Everything currently stored, keyed by relative path
    public IReadOnlyDictionary<string, StoredFile> Files => _files;

    #endregion

    #region Public Methods

    public async Task<string> UploadAsync(AssetType type, string relativePath, Stream content, string contentType)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer);

        _files[relativePath] = new StoredFile(buffer.ToArray(), contentType);

        return relativePath;
    }

    public Task<Stream> DownloadAsync(AssetType type, string relativePath)
    {
        return Task.FromResult<Stream>(new MemoryStream(_files[relativePath].Bytes));
    }

    public Task DeleteAsync(AssetType type, string relativePath)
    {
        _files.Remove(relativePath);

        return Task.CompletedTask;
    }

    public Task<string> GetSignedUrlAsync(AssetType type, string relativePath, TimeSpan expiry)
    {
        return Task.FromResult($"memory://signed/{relativePath}");
    }

    public Task<string> GetPublicUrlAsync(AssetType type, string relativePath)
    {
        return Task.FromResult($"memory://public/{relativePath}");
    }

    #endregion

    #region Nested Types

    public record StoredFile(byte[] Bytes, string ContentType);

    #endregion
}
