// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Persistence/FirestoreRepository.cs
// Version : 1.0.0
// Latest commit: feature/shared-core-primitives
// Author : Gerrah

// Purpose : Provides the Firestore implementation of IRepository<TEntity, Guid>.

using System;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Shared.Persistence;

public class FirestoreRepository<TEntity> : IRepository<TEntity, Guid>
    where TEntity : BaseEntity
{
    #region Fields

    private readonly FirestoreDb _firestoreDb;
    private readonly IClock _clock;
    private readonly string _collectionName;

    #endregion

    #region Constructors

    // Initializes the repository with its Firestore database and collection.
    public FirestoreRepository(
        FirestoreDb firestoreDb,
        IClock clock,
        string collectionName)
    {
        _firestoreDb = firestoreDb;
        _clock = clock;
        _collectionName = collectionName;
    }

    #endregion

    #region Public Methods

    // Retrieves an entity by its identifier.
    public async Task<TEntity?> GetByIdAsync(Guid id)
    {
        var snapshot = await _firestoreDb
            .Collection(_collectionName)
            .Document(id.ToString())
            .GetSnapshotAsync();

        return snapshot.Exists ? snapshot.ConvertTo<TEntity>() : null;
    }

    // Creates a new entity and fails if the document already exists.
    public async Task AddAsync(TEntity entity)
    {
        await _firestoreDb
            .Collection(_collectionName)
            .Document(entity.Id.ToString())
            .CreateAsync(entity);
    }

    // Updates an existing entity and refreshes its modification timestamp.
    public async Task UpdateAsync(TEntity entity)
    {
        entity.Touch(_clock);

        await _firestoreDb
            .Collection(_collectionName)
            .Document(entity.Id.ToString())
            .SetAsync(entity);
    }

    // Deletes an entity by its identifier.
    public async Task DeleteAsync(Guid id)
    {
        await _firestoreDb
            .Collection(_collectionName)
            .Document(id.ToString())
            .DeleteAsync();
    }

    #endregion
}
