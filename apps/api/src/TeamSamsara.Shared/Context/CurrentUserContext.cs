// File: /team-samsara/apps/api/src/TeamSamsara.Shared/Context/CurrentUserContext.cs
// Version: 1.2.0
// Latest commit: feature/identity-profile
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
    public AuthState AuthState { get; set; } = AuthState.Anonymous;
    public long? AuthTime { get; set; }

    #endregion
}
