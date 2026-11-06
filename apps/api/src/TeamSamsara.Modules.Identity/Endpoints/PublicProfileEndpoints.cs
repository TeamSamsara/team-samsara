// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Endpoints/PublicProfileEndpoints.cs
// Version : 1.1.0
// Latest commit: task/web-member-profile
// Author : Gerrah
// Purpose : Rate-limited lookup of a member's profile by handle, for verified members only.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Shared.Http;

namespace TeamSamsara.Modules.Identity.Endpoints;

public static class PublicProfileEndpoints
{
    #region Fields

    // Name of the per-IP rate limit policy that guards profile lookups
    public const string RateLimitPolicyName = "PublicRead";

    #endregion

    #region Public Methods

    // Maps the lookup for verified members: signed-out callers get 401, unverified callers get 403
    public static void Map(IEndpointRouteBuilder routes)
    {
        var group = routes
            .MapGroup(IdentityRoutes.PublicProfileGroup)
            .AddEndpointFilter<MemberOnlyEndpointFilter>()
            .RequireRateLimiting(RateLimitPolicyName);

        group.MapGet(IdentityRoutes.PublicProfileByHandle, GetAsync);
    }

    #endregion

    #region Private Methods

    // Returns the public profile, or 404 when no member owns the handle
    private static async Task<IResult> GetAsync(string handle, IProfileService profiles)
    {
        var profile = await profiles.GetPublicAsync(handle);

        return profile is null ? Results.NotFound() : Results.Ok(profile);
    }

    #endregion
}
