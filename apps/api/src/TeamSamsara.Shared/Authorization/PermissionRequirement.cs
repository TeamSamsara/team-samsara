// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Authorization/PermissionAuthorization.cs
// Version : 1.0.0
// Latest commit: feature/shared-core-primitives
// Author : Gerrah

// Purpose : Provides the authorization components for permission-based policies.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Shared.Authorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    #region Properties

    public string Permission { get; }

    #endregion

    #region Constructors

    // Initializes the requirement with the required permission.
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }

    #endregion
}

public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    #region Constructors

    // Initializes the policy provider with the application authorization options.
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options)
    {
    }

    #endregion

    #region Public Methods

    // Resolves permission policies dynamically from their policy names.
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(RequirePermissionAttribute.PolicyPrefix))
        {
            return await base.GetPolicyAsync(policyName);
        }

        var permission = policyName[RequirePermissionAttribute.PolicyPrefix.Length..];

        return new AuthorizationPolicyBuilder()
            .AddRequirements(new PermissionRequirement(permission))
            .Build();
    }

    #endregion
}

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    #region Fields

    private readonly IPermissionChecker _permissionChecker;
    private readonly ICurrentUserContext _currentUserContext;

    #endregion

    #region Constructors

    // Initializes the handler with the permission checker and current user context.
    public PermissionAuthorizationHandler(
        IPermissionChecker permissionChecker,
        ICurrentUserContext currentUserContext)
    {
        _permissionChecker = permissionChecker;
        _currentUserContext = currentUserContext;
    }

    #endregion

    #region Protected Methods

    // Succeeds when the current user has the required permission.
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var hasPermission = await _permissionChecker.HasPermissionAsync(
            _currentUserContext,
            requirement.Permission);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }

    #endregion
}
