// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Assets/Repositories/IAssetMetadataStore.cs
// Version : 1.0.1
// Latest commit: feature/assets-module
// Author : Gerrah
// Purpose : Looks up, records, and removes asset metadata, independent of where that metadata is stored.

using System.Threading.Tasks;
using TeamSamsara.Modules.Assets.Models;

namespace TeamSamsara.Modules.Assets.Repositories;

public interface IAssetMetadataStore
{
    #region Public Methods

    // Retrieves asset metadata by id, or null if no asset has that id
    public Task<AssetMetadata?> GetByIdAsync(string id);

    // Creates a new asset metadata record
    public Task CreateAsync(AssetMetadata metadata);

    // Deletes an asset metadata record by id
    public Task DeleteAsync(string id);

    #endregion
}
