// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Persistence/IRepository.cs
// Version : 1.0.0
// Latest commit: feature/shared-core-primitives
// Author : Gerrah

// Purpose : Defines the standard persistence contract for entities.

using System.Threading.Tasks;

namespace TeamSamsara.Shared.Persistence;

public interface IRepository<TEntity, in TId> where TEntity : BaseEntity
{
    #region  Public Methods

    // Retrieves an entity by its identifier
    public Task<TEntity?> GetByIdAsync(TId id);

    // Add new entity
    public Task AddAsync(TEntity entity);

    // Persist change to an existing entity
    public Task UpdateAsync(TEntity entity);

    // Remove an entity by its identifier
    public Task DeleteAsync(TId id);
    #endregion
}
