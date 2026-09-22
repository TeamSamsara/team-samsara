// File: /team-samsara/apps/api/src/TeamSamsara.Shared/Context/GuidV7IdGenerator.cs
// Version: 1.0.0
// Latest commit: feature/shared-core-primitives
// Author: Gerrah
//
// Purpose: Provides the GUID v7 implementation of IIdGenerator.

using System;

namespace TeamSamsara.Shared.Context;

public class GuidV7IdGenerator : IIdGenerator
{
    #region Public Methods

    public Guid NewId() => Guid.CreateVersion7();

    #endregion
}
