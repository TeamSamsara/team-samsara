// File: /team-samsara/apps/api/src/TeamSamsara.Shared/Context/ICurrentUserContext.cs
// Version: 1.2.0
// Latest commit: feature/identity-profile
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
    public AuthState AuthState { get; }

    // When the token's sign-in happened (Unix seconds), or null if unauthenticated
    public long? AuthTime { get; }

    #endregion
}
