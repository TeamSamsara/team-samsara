// File : /team-samsara/apps/api/src/TeamSamsara.Modules.PingPong/PingPongModule.cs
// Version : 1.0.1
// Latest commit: feature/string-magic-value-conventions
// Author : Gerrah
// Purpose : Provides a minimal module for validating module registration and authorization.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeamSamsara.Shared.Authorization;
using TeamSamsara.Shared.Modules;

namespace TeamSamsara.Modules.PingPong;

public class PingPongModule : IModule
{
    #region Public Methods

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Nothing to register - PingPong has no repository, no settings,
        // and no dependencies of its own.
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(PingPongConstants.PingRoute, () => PingPongConstants.PongResponse);

        endpoints
            .MapGet(PingPongConstants.PingSecureRoute, () => PingPongConstants.PongResponse)
            .RequireAuthorization(new RequirePermissionAttribute(Permissions.PingPongPing).Policy!);
    }

    #endregion
}
