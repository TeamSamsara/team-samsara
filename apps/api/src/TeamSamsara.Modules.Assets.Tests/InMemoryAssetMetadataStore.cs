// File: /team-samsara/apps/api/src/TeamSamsara.Modules.Assets.Tests/Fakes/InMemoryAssetMetadataStore.cs
// Version: 1.0.0
// Latest commit: feature/identity-profile
// Author: Gerrah
//
// Purpose: An in-memory asset metadata store.

using TeamSamsara.Modules.Assets.Models;
using TeamSamsara.Modules.Assets.Repositories;

namespace TeamSamsara.Modules.Assets.Tests.Fakes;

public class InMemoryAssetMetadataStore : IAssetMetadataStore
{
    #region Fields

    private readonly Dictionary<string, AssetMetadata> _assets = new();

    #endregion

    #region Properties

    // Every metadata record currently stored, keyed by asset id
    public IReadOnlyDictionary<string, AssetMetadata> Assets => _assets;

    #endregion

    #region Public Methods

    public Task<AssetMetadata?> GetByIdAsync(string id)
    {
        return Task.FromResult(_assets.TryGetValue(id, out var metadata) ? metadata : null);
    }

    public Task<IReadOnlyList<AssetMetadata>> ListAsync(AssetType? type)
    {
        IReadOnlyList<AssetMetadata> assets = _assets.Values
            .Where(asset => type is null || asset.type == type)
            .ToList();

        return Task.FromResult(assets);
    }

    public Task CreateAsync(AssetMetadata metadata)
    {
        if (!_assets.TryAdd(metadata.Id, metadata))
        {
            throw new InvalidOperationException($"Asset '{metadata.Id}' already exists.");
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id)
    {
        _assets.Remove(id);

        return Task.CompletedTask;
    }

    #endregion
}
