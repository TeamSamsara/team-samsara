// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Modules/IModule.cs
// Version : 1.0.0
// Latest commit: feature/shared-core-primitives
// Author : Gerrah

// Purpose : Defines the contract for API module registration and endpoint mapping.

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TeamSamsara.Shared.Modules;

public interface IModule
{
    #region Public Methods

    // Registers the module's services and dependencies.
    void RegisterServices(
        IServiceCollection services,
        IConfiguration configuration);

    // Maps the module's HTTP endpoints.
    void MapEndpoints(IEndpointRouteBuilder endpoints);

    #endregion
}
