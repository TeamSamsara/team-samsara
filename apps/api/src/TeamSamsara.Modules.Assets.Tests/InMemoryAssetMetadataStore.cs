// File: /team-samsara/apps/api/src/TeamSamsara.Modules.Assets.Tests/Fakes/InMemoryAssetMetadataStore.cs
// Version: 1.1.0
// Latest commit: feat/account-purge
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

    // When true, deleting a record throws, as a failing database would
    public bool FailDeletes { get; set; }

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
        if (FailDeletes)
        {
            throw new InvalidOperationException("Simulated database failure.");
        }

        _assets.Remove(id);

        return Task.CompletedTask;
    }

    #endregion
}
