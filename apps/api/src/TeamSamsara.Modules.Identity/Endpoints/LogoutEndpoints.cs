// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Endpoints/LogoutEndpoints.cs
// Version : 1.0.0
// Latest commit: feat/logout
// Author : Gerrah
// Purpose : HTTP endpoints for a verified member signing out of this device or of every device.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Shared.Context;
using TeamSamsara.Shared.Http;

namespace TeamSamsara.Modules.Identity.Endpoints;

public static class LogoutEndpoints
{
    #region Public Methods

    // Maps the logout endpoints onto the given route group, open to verified members only.
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPost(IdentityRoutes.Logout, SignOutAsync)
            .AddEndpointFilter<MemberOnlyEndpointFilter>();

        routes.MapPost(IdentityRoutes.LogoutAll, SignOutEverywhereAsync)
            .AddEndpointFilter<MemberOnlyEndpointFilter>();
    }

    #endregion

    #region Private Methods

    // Ends the sign-in this request was made with; other devices stay signed in.
    private static async Task<IResult> SignOutAsync(
        ICurrentUserContext context,
        ISessionService sessions,
        CancellationToken cancellationToken)
    {
        if (context.UserId is null || context.AuthTime is null)
        {
            return Results.Unauthorized();
        }

        await sessions.SignOutAsync(context.UserId, context.AuthTime.Value, cancellationToken);

        return Results.NoContent();
    }

    // Revokes every refresh token and forgets every verified sign-in of the member.
    private static async Task<IResult> SignOutEverywhereAsync(
        ICurrentUserContext context,
        ISessionService sessions,
        CancellationToken cancellationToken)
    {
        if (context.UserId is null)
        {
            return Results.Unauthorized();
        }

        await sessions.SignOutEverywhereAsync(context.UserId, cancellationToken);

        return Results.NoContent();
    }

    #endregion
}
