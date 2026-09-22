// File : /team-samsara/apps/api/src/TeamSamsara.Modules.PingPong/PingPongModule.cs
// Version : 1.0.0
// Latest commit: feature/pingpong-module-template
// Author : Gerrah
// Purpose : Provides a minimal module for validating module registration and authorization.

using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
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
        // Nothing to register — PingPong has no repository, no settings,
        // and no dependencies of its own.
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/ping", () => "pong");

        endpoints
            .MapGet("/ping/secure", () => "pong")
            .RequireAuthorization(new RequirePermissionAttribute("pingpong.ping").Policy!);
    }

    #endregion
}
