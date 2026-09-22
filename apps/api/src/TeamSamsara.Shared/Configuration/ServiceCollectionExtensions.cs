// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Configuration/ServiceCollectionExtensions.cs
// Version : 1.0.0
// Latest commit: feature/shared-core-primitives
// Author : Gerrah

// Purpose: Provides the standard configuration binding and startup validation pattern.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace TeamSamsara.Shared.Configuration;

public static class ServiceCollectionExtensions
{
    #region Public Methods

    public static IServiceCollection AddValidatedOptions<T>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where T : class
    {
        services
            .AddOptions<T>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    #endregion
}
