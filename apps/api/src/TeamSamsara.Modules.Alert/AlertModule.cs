// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Alert/AlertModule.cs
// Version : 1.0.0
// Latest commit: feature/alerts-module
// Author : Gerrah
// Purpose : Registers the Alert module's services. Exposes no HTTP endpoints - alerts are
// triggered by other modules through IAlertSender, never by a request.

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeamSamsara.Shared.Alert;
using TeamSamsara.Shared.Modules;

namespace TeamSamsara.Modules.Alert;

public class AlertModule : IModule
{
    #region Public Methods

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAlertSender, AlertSender>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Nothing to map - the Alert module has no HTTP surface.
    }

    #endregion
}
