// File: /team-samsara/apps/api/src/TeamSamsara.Shared/Context/IIdGenerator.cs
// Version: 1.0.0
// Latest commit: feature/shared-core-primitives
// Author: Gerrah
//
// Purpose: Abstracts entity ID generation behind a shared application-wide contract.

using System;

namespace TeamSamsara.Shared.Context;

public interface IIdGenerator
{
    #region  Public Methods
    public Guid NewId();
    #endregion
}
