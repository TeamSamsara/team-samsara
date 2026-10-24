// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Endpoints/PublicProfileEndpoints.cs
// Version : 1.0.0
// Latest commit: feat/api-profile-onboarding
// Author : Gerrah
// Purpose : Anonymous, rate-limited lookup of a member's public profile by handle.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TeamSamsara.Modules.Identity.Handlers;

namespace TeamSamsara.Modules.Identity.Endpoints;

public static class PublicProfileEndpoints
{
    #region Fields

    // Name of the per-IP rate limit policy that guards public profile lookups
    public const string RateLimitPolicyName = "PublicRead";

    #endregion

    #region Public Methods

    // Maps the lookup for callers who are not signed in
    public static void Map(IEndpointRouteBuilder routes)
    {
        var group = routes
            .MapGroup(IdentityRoutes.PublicProfileGroup)
            .AllowAnonymous()
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
