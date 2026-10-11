// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/NotificationModule.cs
// Version : 1.0.0
// Latest commit: feat/notification-module-scaffold
// Author : Gerrah
// Purpose : Registers the Notification module's services and HTTP endpoints; both are empty until the module is built.

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeamSamsara.Shared.Modules;

namespace TeamSamsara.Modules.Notification;

public class NotificationModule : IModule
{
    #region Public Methods

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Nothing to register yet.
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Nothing to map yet.
    }

    #endregion
}
