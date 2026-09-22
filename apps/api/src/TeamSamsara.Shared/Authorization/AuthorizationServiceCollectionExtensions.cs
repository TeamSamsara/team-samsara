// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Authorization/AuthorizationServiceCollectionExtensions.cs
// Version : 1.0.1
// Latest commit: feature/api-host
// Author : Gerrah
// Purpose : Registers the permanent authorization pieces (policy provider,
// handler, permission checker, current user context) as one call from
// Program.cs, matching the AddFirestore/AddFirebaseStorage pattern already
// used elsewhere in Shared. Deliberately does not register any
// authentication scheme — that stays in Program.cs since the real Firebase
// scheme (item 10) will differ from the temporary one used until then.

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Shared.Authorization;

public static class AuthorizationServiceCollectionExtensions
{
    #region Public Methods

    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IPermissionChecker, DefaultPermissionChecker>();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();

        return services;
    }

    #endregion
}
