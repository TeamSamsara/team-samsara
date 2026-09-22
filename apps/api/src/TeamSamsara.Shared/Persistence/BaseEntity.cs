// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Persistence/BaseEntity.cs
// Version : 1.0.0
// Latest commit: feature/shared-core-primitives
// Author : Gerrah

// Purpose : Provides the shared identity and audit fields for persisted entities.

using System;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Shared.Persistence;

public abstract class BaseEntity
{
    #region Properties
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    #endregion

    #region Public Methods

    // Updates the entity's modification audit fields
    public void Touch(IClock clock)
    {
        UpdatedAt = clock.UtcNow;
    }

    #endregion
}
