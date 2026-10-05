// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Logging/SerilogHostBuilderExtensions.cs
// Version : 1.0.1
// Latest commit: feature/string-magic-value-conventions
// Author : Gerrah

// Purpose : Configures the application's Serilog logging pipeline.

using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace TeamSamsara.Shared.Logging;

public static class SerilogHostBuilderExtensions
{
    #region Public Methods

    // Configures Serilog with environment-specific console output.
    public static IHostBuilder AddSamsaraLogging(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty(LogPropertyNames.Environment, context.HostingEnvironment.EnvironmentName)
                .Enrich.WithProperty(LogPropertyNames.Service, "TeamSamsara.Api");

            if (context.HostingEnvironment.IsDevelopment())
            {
                loggerConfiguration.WriteTo.Console();
            }
            else
            {
                loggerConfiguration.WriteTo.Console(new CompactJsonFormatter());
            }
        });
    }

    #endregion
}
