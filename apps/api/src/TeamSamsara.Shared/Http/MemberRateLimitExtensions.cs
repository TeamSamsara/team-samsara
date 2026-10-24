// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Http/MemberRateLimitExtensions.cs
// Version : 1.0.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Lets a member-only endpoint opt into a per-member rate limit with one call.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace TeamSamsara.Shared.Http;

public static class MemberRateLimitExtensions
{
    #region Public Methods

    // Limits each signed-in member to permitLimit calls every windowSeconds on this endpoint
    public static TBuilder RequirePerMemberRateLimit<TBuilder>(
        this TBuilder builder,
        int permitLimit,
        int windowSeconds)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.AddEndpointFilter(new MemberRateLimitFilter(permitLimit, windowSeconds));
    }

    #endregion
}
