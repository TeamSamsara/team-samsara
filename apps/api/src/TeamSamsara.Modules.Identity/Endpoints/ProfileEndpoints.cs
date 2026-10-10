// File: /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Endpoints/ProfileEndpoints.cs
// Version: 1.0.1
// Latest commit: fix/profile-guest-before-form
// Author: Gerrah
//
// Purpose: HTTP endpoints for a member's own profile: read, edit name and bio, set or remove images.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Shared.Assets;
using TeamSamsara.Shared.Context;
using TeamSamsara.Shared.Http;

namespace TeamSamsara.Modules.Identity.Endpoints;

public static class ProfileEndpoints
{
    #region Fields

    private const string ImageFieldName = "file";

    #endregion

    #region Public Methods

    // Maps the profile endpoints onto the given route group, open to verified members only
    public static void Map(IEndpointRouteBuilder routes)
    {
        var group = routes
            .MapGroup(IdentityRoutes.ProfileGroup)
            .AddEndpointFilter<MemberOnlyEndpointFilter>();

        group.MapGet(IdentityRoutes.ProfileRoot, GetAsync);
        group.MapPut(IdentityRoutes.ProfileRoot, UpdateAsync);

        group.MapPut(IdentityRoutes.ProfilePicture, SetPictureAsync);
        group.MapDelete(IdentityRoutes.ProfilePicture, ClearPictureAsync);

        group.MapPut(IdentityRoutes.ProfileBanner, SetBannerAsync);
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
        HttpRequest request,
        ICurrentUserContext context,
        IProfileService profiles)
    {
        return SetImageAsync(MemberImageKind.ProfilePicture, request, context, profiles);
    }

    // Removes the caller's profile picture
    private static Task<IResult> ClearPictureAsync(ICurrentUserContext context, IProfileService profiles)
    {
        return RunAsync(context, userId => profiles.ClearImageAsync(userId, MemberImageKind.ProfilePicture));
    }

    // Sets the caller's banner from an uploaded image
    private static Task<IResult> SetBannerAsync(
        HttpRequest request,
        ICurrentUserContext context,
        IProfileService profiles)
    {
        return SetImageAsync(MemberImageKind.Banner, request, context, profiles);
    }

    // Removes the caller's banner
    private static Task<IResult> ClearBannerAsync(ICurrentUserContext context, IProfileService profiles)
    {
        return RunAsync(context, userId => profiles.ClearImageAsync(userId, MemberImageKind.Banner));
    }

    // Reads the uploaded file from the request and hands it to the profile service as the caller's
    // image of this kind. The form is read here, not bound as a parameter, because binding happens
    // before the member-only filter and would let non-members get a 400 instead of a 401 or 403.
    private static async Task<IResult> SetImageAsync(
        MemberImageKind kind,
        HttpRequest request,
        ICurrentUserContext context,
        IProfileService profiles)
    {
        var file = await ReadFileAsync(request);
        if (file is null)
        {
            return Results.BadRequest();
        }

        await using var content = file.OpenReadStream();

        return await RunAsync(context, userId => profiles.SetImageAsync(userId, kind, content));
    }

    // Returns the uploaded file, or null when the request is not a form or carries no such file
    private static async Task<IFormFile?> ReadFileAsync(HttpRequest request)
    {
        if (!request.HasFormContentType)
        {
            return null;
        }

        try
        {
            var form = await request.ReadFormAsync();

            return form.Files.GetFile(ImageFieldName);
        }
        catch (InvalidDataException)
        {
            return null;
        }
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
