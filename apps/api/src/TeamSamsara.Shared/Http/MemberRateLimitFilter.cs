// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Http/MemberRateLimitFilter.cs
// Version : 1.0.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Limits how often one signed-in member may call an endpoint, counting per member
// instead of per IP. It runs after authentication, which the rate limiter middleware does not.

using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TeamSamsara.Shared.Context;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace TeamSamsara.Shared.Http;

public class MemberRateLimitFilter : IEndpointFilter
{
    #region Fields

    private readonly PartitionedRateLimiter<string> _limiter;

    #endregion

    #region Constructors

    // Builds a fixed window counter per member: at most permitLimit calls every windowSeconds.
    // The counts live in memory, so they are per server instance and reset on a restart.
    public MemberRateLimitFilter(int permitLimit, int windowSeconds)
    {
        _limiter = PartitionedRateLimiter.Create<string, string>(userId =>
            RateLimitPartition.GetFixedWindowLimiter(
                userId,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromSeconds(windowSeconds),
                    QueueLimit = 0
                }));
    }

    #endregion

    #region Public Methods

    // Lets the call through while the member is within the limit, otherwise answers 429 with
    // Retry-After. A caller who is not signed in is left to the authentication filters.
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var userId = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserContext>().UserId;

        if (userId is null)
        {
            return await next(context);
        }

        using var lease = _limiter.AttemptAcquire(userId);

        if (lease.IsAcquired)
        {
            return await next(context);
        }

        WriteRetryAfter(context.HttpContext, lease);

        return HttpResults.StatusCode(StatusCodes.Status429TooManyRequests);
    }

    #endregion

    #region Private Methods

    // Tells the client when to retry, when the limiter knows
    private static void WriteRetryAfter(HttpContext context, RateLimitLease lease)
    {
        if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            var seconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
            context.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
        }
    }

    #endregion
}
