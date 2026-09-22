// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Authorization/IPermissionChecker.cs
// Version : 1.0.0
// Latest commit: feature/shared-core-primitives
// Author : Gerrah
// Purpose : Checks whether a user has a given permission.

using System.Threading.Tasks;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Shared.Authorization;

public interface IPermissionChecker
{
    #region Public Methods

    Task<bool> HasPermissionAsync(ICurrentUserContext user, string permission);

    #endregion
}
