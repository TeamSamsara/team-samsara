// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Authorization/RequirePermissionAttribute.cs
// Version : 1.0.0
// Latest commit: feature/shared-core-primitives
// Author : Gerrah
// Purpose : Applied to an endpoint to require a specific permission string.
// Sets Policy to "Permission:{permission}"

using System;
using Microsoft.AspNetCore.Authorization;

namespace TeamSamsara.Shared.Authorization;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequirePermissionAttribute : AuthorizeAttribute
{
    #region Fields

    public const string PolicyPrefix = "Permission:";

    #endregion

    #region Constructors

    public RequirePermissionAttribute(string permission)
    {
        Policy = $"{PolicyPrefix}{permission}";
    }

    #endregion
}
