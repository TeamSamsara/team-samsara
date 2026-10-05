// File: /team-samsara/apps/api/src/TeamSamsara.Shared/Context/IClock.cs
// Version: 1.0.0
// Latest commit: feature/shared-core-primitives
// Author: Gerrah
//
// Purpose: Abstracts the current time for testable time-dependent logic.

using System;

namespace TeamSamsara.Shared.Context;

public interface IClock
{
    #region Properties
    public DateTimeOffset UtcNow { get; }
    #endregion
}
