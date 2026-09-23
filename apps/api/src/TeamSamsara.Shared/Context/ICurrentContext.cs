// File: /team-samsara/apps/api/src/TeamSamsara.Shared/Context/ICurrentUserContext.cs
// Version: 1.0.0
// Latest commit: feature/shared-core-primitives
// Author: Gerrah
//
// Purpose: Provides the identity of the current request independently of the API host.

namespace TeamSamsara.Shared.Context;

public interface ICurrentUserContext
{
    #region Properties

    public bool IsAuthenticated { get; }
    public string? UserId { get; }
    public AccessLevel AccessLevel { get; }

    #endregion
}
