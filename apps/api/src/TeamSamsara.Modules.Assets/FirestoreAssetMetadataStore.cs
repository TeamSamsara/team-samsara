// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Assets/Repositories/FirestoreAssetMetadataStore.cs
// Version : 1.0.2
// Latest commit: feature/assets-module
// Author : Gerrah
// Purpose : Firestore implementation of the asset metadata store.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using TeamSamsara.Modules.Assets.Models;

namespace TeamSamsara.Modules.Assets.Repositories;

public class FirestoreAssetMetadataStore : IAssetMetadataStore
{
    #region Fields
    private readonly FirestoreDb _firestoreDb;
    #endregion

    #region Constructors

    // Initialize the store with its Firestore database
    public FirestoreAssetMetadataStore(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }
    #endregion

    #region Public Methods

    // Retrieves asset metadata by id, or null if no asset has that id
    public async Task<AssetMetadata?> GetByIdAsync(string id)
    {
        var snapshot = await _firestoreDb
        .Collection(AssetsFirestoreCollections.Assets)
        .Document(id)
        .GetSnapshotAsync();

        return snapshot.Exists ? snapshot.ConvertTo<AssetMetadata>() : null;
    }

    // Retrieves all asset metadata, optionally filtered by type
    public async Task<IReadOnlyList<AssetMetadata>> ListAsync(AssetType? type)
    {
        Query query = _firestoreDb.Collection(AssetsFirestoreCollections.Assets);

        if (type is not null)
        {
            query = query.WhereEqualTo(nameof(AssetMetadata.type), type);
        }

        var snapshot = await query.GetSnapshotAsync();

        return snapshot.Documents.Select(document => document.ConvertTo<AssetMetadata>()).ToList();
    }

    // Creates a new asset metadata record
    public async Task CreateAsync(AssetMetadata metadata)
    {
        await _firestoreDb
        .Collection(AssetsFirestoreCollections.Assets)
        .Document(metadata.Id)
        .CreateAsync(metadata);
    }

    // Deletes an asset metadata record by id
    public async Task DeleteAsync(string id)
    {
        await _firestoreDb
        .Collection(AssetsFirestoreCollections.Assets)
        .Document(id)
        .DeleteAsync();
    }

    #endregion
}
