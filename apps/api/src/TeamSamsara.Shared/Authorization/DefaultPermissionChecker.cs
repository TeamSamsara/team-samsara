// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Authorization/DefaultPermissionChecker.cs
// Version : 1.0.0
// Latest commit: feature/shared-core-primitives
// Author : Gerrah
// Purpose : Provides the initial permission-checking implementation.

using System.Threading.Tasks;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Shared.Authorization;

public class DefaultPermissionChecker : IPermissionChecker
{
    #region Public Methods

    // permission is intentionally unused in this Phase 1 implementation.
    public Task<bool> HasPermissionAsync(ICurrentUserContext user, string permission)
    {
        return Task.FromResult(user.AccessLevel == AccessLevel.Admin);
    }

    #endregion
}
