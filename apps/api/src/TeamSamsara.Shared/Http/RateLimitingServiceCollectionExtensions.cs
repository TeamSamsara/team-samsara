// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Http/RateLimitingServiceCollectionExtensions.cs
// Version : 1.1.0
// Latest commit: feat/contact-module
// Author : Gerrah
// Purpose : Registers the per-IP rate limit policies that signed-out endpoints opt into with
// RequireRateLimiting(policyName).

using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace TeamSamsara.Shared.Http;

public static class RateLimitingServiceCollectionExtensions
{
    #region Fields

    private const string UnknownClient = "unknown";

    #endregion

    #region Public Methods

    // Adds the PerIp policy and the shared 429 answer: a fixed window per client IP, with Retry-After
    public static IServiceCollection AddSamsaraRateLimiting(
        this IServiceCollection services,
        RateLimitSettings settings)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRetryAfterAsync;
        });

        return services.AddPerIpRateLimitPolicy(
            RateLimitSettings.PerIpPolicyName,
            settings.PermitLimit,
            settings.WindowSeconds);
    }

    // Adds a named fixed-window policy that counts requests per client IP, separate from the other policies
    public static IServiceCollection AddPerIpRateLimitPolicy(
        this IServiceCollection services,
        string policyName,
        int permitLimit,
        int windowSeconds)
    {
        services.AddRateLimiter(options =>
        {
            options.AddPolicy(
                policyName,
                context => RateLimitPartition.GetFixedWindowLimiter(
                    GetClientKey(context),
                    _ => CreateWindow(permitLimit, windowSeconds)));
        });

        return services;
    }

    #endregion

    #region Private Methods

    // Builds the window options for one limit and length
    private static FixedWindowRateLimiterOptions CreateWindow(int permitLimit, int windowSeconds)
    {
        return new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromSeconds(windowSeconds),
            QueueLimit = 0
        };
    }

    // Must run after forwarded headers are applied, or every caller shares the proxy's address.
    private static string GetClientKey(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? UnknownClient;
    }

    // Tells the client when to retry
    private static ValueTask WriteRetryAfterAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            var seconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
            context.HttpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
        }

        return ValueTask.CompletedTask;
    }

    #endregion
}
