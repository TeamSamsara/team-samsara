// File: /team-samsara/apps/api/src/TeamSamsara.Shared/Context/CurrentUserContext.cs
// Version: 1.0.0
// Latest commit: feature/shared-core-primitives
// Author: Gerrah
//
// Purpose: Provides the default implementation of ICurrentUserContext.

namespace TeamSamsara.Shared.Context;

public class CurrentUserContext : ICurrentUserContext
{
    #region Properties

    public bool IsAuthenticated { get; set; }
    public string? UserId { get; set; }
    public AccessLevel AccessLevel { get; set; } = AccessLevel.Guest;

    #endregion
}
