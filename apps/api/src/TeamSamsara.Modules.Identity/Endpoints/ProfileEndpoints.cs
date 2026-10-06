// File: /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Endpoints/ProfileEndpoints.cs
// Version: 1.0.0
// Latest commit: feature/identity-profile
// Author: Gerrah
//
// Purpose: HTTP endpoints for a member's own profile: read, edit name and bio, set or remove images.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Shared.Assets;
using TeamSamsara.Shared.Context;
using TeamSamsara.Shared.Http;

namespace TeamSamsara.Modules.Identity.Endpoints;

public static class ProfileEndpoints
{
    #region Public Methods

    // Maps the profile endpoints onto the given route group, open to verified members only
    public static void Map(IEndpointRouteBuilder routes)
    {
        var group = routes
            .MapGroup(IdentityRoutes.ProfileGroup)
            .AddEndpointFilter<MemberOnlyEndpointFilter>();

        group.MapGet(IdentityRoutes.ProfileRoot, GetAsync);
        group.MapPut(IdentityRoutes.ProfileRoot, UpdateAsync);

        // Callers authenticate with a bearer token, not a cookie, so there is no ambient
        // credential for a forged form to use - antiforgery protection doesn't apply here.
        group.MapPut(IdentityRoutes.ProfilePicture, SetPictureAsync).DisableAntiforgery();
        group.MapDelete(IdentityRoutes.ProfilePicture, ClearPictureAsync);

        group.MapPut(IdentityRoutes.ProfileBanner, SetBannerAsync).DisableAntiforgery();
        group.MapDelete(IdentityRoutes.ProfileBanner, ClearBannerAsync);
    }

    #endregion

    #region Private Methods

    // Returns the caller's profile
    private static Task<IResult> GetAsync(ICurrentUserContext context, IProfileService profiles)
    {
        return RunAsync(context, userId => profiles.GetAsync(userId));
    }

    // Replaces the caller's display name and bio
    private static Task<IResult> UpdateAsync(
        UpdateProfileRequest request,
        ICurrentUserContext context,
        IProfileService profiles)
    {
        return RunAsync(context, userId => profiles.UpdateAsync(userId, request));
    }

    // Sets the caller's profile picture from an uploaded image
    private static Task<IResult> SetPictureAsync(
        [FromForm] IFormFile file,
        ICurrentUserContext context,
        IProfileService profiles)
    {
        return SetImageAsync(MemberImageKind.ProfilePicture, file, context, profiles);
    }

    // Removes the caller's profile picture
    private static Task<IResult> ClearPictureAsync(ICurrentUserContext context, IProfileService profiles)
    {
        return RunAsync(context, userId => profiles.ClearImageAsync(userId, MemberImageKind.ProfilePicture));
    }

    // Sets the caller's banner from an uploaded image
    private static Task<IResult> SetBannerAsync(
        [FromForm] IFormFile file,
        ICurrentUserContext context,
        IProfileService profiles)
    {
        return SetImageAsync(MemberImageKind.Banner, file, context, profiles);
    }

    // Removes the caller's banner
    private static Task<IResult> ClearBannerAsync(ICurrentUserContext context, IProfileService profiles)
    {
        return RunAsync(context, userId => profiles.ClearImageAsync(userId, MemberImageKind.Banner));
    }

    // Hands the uploaded file to the profile service as the caller's image of this kind
    private static async Task<IResult> SetImageAsync(
        MemberImageKind kind,
        IFormFile file,
        ICurrentUserContext context,
        IProfileService profiles)
    {
        await using var content = file.OpenReadStream();

        return await RunAsync(context, userId => profiles.SetImageAsync(userId, kind, content));
    }

    // Runs a profile operation for the caller and answers with its outcome and status code
    private static async Task<IResult> RunAsync(
        ICurrentUserContext context,
        Func<string, Task<ProfileResult>> operation)
    {
        if (context.UserId is null)
        {
            return Results.Unauthorized();
        }

        var result = await operation(context.UserId);

        return Results.Json(result, statusCode: IdentityStatusCodes.For(result.Status));
    }

    #endregion
}
