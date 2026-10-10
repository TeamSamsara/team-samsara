// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Http/RateLimitingServiceCollectionExtensions.cs
// Version : 1.0.0
// Latest commit: feat/password-reset-endpoints
// Author : Gerrah
// Purpose : Registers the per-IP rate limit policy that signed-out endpoints opt into with
// RequireRateLimiting(RateLimitSettings.PerIpPolicyName).

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

    // Adds the PerIp policy: a fixed window per client IP, answered with 429 and Retry-After
    public static IServiceCollection AddSamsaraRateLimiting(
        this IServiceCollection services,
        RateLimitSettings settings)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRetryAfterAsync;

            options.AddPolicy(
                RateLimitSettings.PerIpPolicyName,
                context => RateLimitPartition.GetFixedWindowLimiter(
                    GetClientKey(context),
                    _ => CreateWindow(settings)));
        });

        return services;
    }

    #endregion

    #region Private Methods

    private static FixedWindowRateLimiterOptions CreateWindow(RateLimitSettings settings)
    {
        return new FixedWindowRateLimiterOptions
        {
            PermitLimit = settings.PermitLimit,
            Window = TimeSpan.FromSeconds(settings.WindowSeconds),
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
