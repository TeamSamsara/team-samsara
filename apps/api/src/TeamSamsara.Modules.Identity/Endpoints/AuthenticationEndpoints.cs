// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Endpoints/AuthenticationEndpoints.cs
// Version : 1.1.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : HTTP endpoints for logging in: check (verifies a recognized sign-in or sends the
// new-device challenge), confirm the challenge code, and resend it. The caller is the
// freshly signed-in account; its sign-in is identified by the token's auth_time.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Endpoints;

public static class AuthenticationEndpoints
{
    #region Public Methods

    // Maps the login endpoints onto the given (already authorized) route group
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPost(IdentityRoutes.LoginCheck, CheckAsync);
        routes.MapPost(IdentityRoutes.LoginConfirm, ConfirmAsync);
        routes.MapPost(IdentityRoutes.LoginResend, ResendAsync);
        routes.MapGet(IdentityRoutes.Me, GetMe);
    }

    #endregion

    #region Private Methods

    // Verifies the sign-in, or sends the new-device challenge code
    private static async Task<IResult> CheckAsync(
        ICurrentUserContext context,
        IAuthenticationService authentication,
        CancellationToken cancellationToken)
    {
        if (context.UserId is null || context.AuthTime is null)
        {
            return Results.Unauthorized();
        }

        var result = await authentication.CheckSignInAsync(
            context.UserId, context.AuthTime.Value, cancellationToken);

        return Results.Json(result, statusCode: IdentityStatusCodes.For(result.Status));
    }

    // Checks the challenge code and, if valid, verifies the sign-in
    private static async Task<IResult> ConfirmAsync(
        ConfirmCodeRequest request,
        ICurrentUserContext context,
        IAuthenticationService authentication,
        CancellationToken cancellationToken)
    {
        if (context.UserId is null || context.AuthTime is null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return Results.BadRequest();
        }

        var status = await authentication.ConfirmChallengeAsync(
            context.UserId, context.AuthTime.Value, request.Code, cancellationToken);

        return Results.Json(
            new StatusResponse<AuthenticationStatus>(status),
            statusCode: IdentityStatusCodes.For(status));
    }

    // Sends a fresh challenge code
    private static async Task<IResult> ResendAsync(
        ICurrentUserContext context,
        IAuthenticationService authentication,
        CancellationToken cancellationToken)
    {
        if (context.UserId is null)
        {
            return Results.Unauthorized();
        }

        var result = await authentication.ResendChallengeAsync(context.UserId, cancellationToken);

        return Results.Json(result, statusCode: IdentityStatusCodes.For(result.Status));
    }

    // Reports who the API considers the caller to be: the effective access level, so a
    // member whose sign-in is not verified yet shows up as a Guest
    private static IResult GetMe(ICurrentUserContext context)
    {
        if (context.UserId is null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(new AccountSummary(context.UserId, context.AccessLevel));
    }

    #endregion
}
