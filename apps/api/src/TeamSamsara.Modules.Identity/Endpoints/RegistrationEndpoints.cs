// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Endpoints/RegistrationEndpoints.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : HTTP endpoints for registration: start (sends the first code), resend, and
// confirm (completes registration). The caller is the signed-in, not-yet-confirmed account
// identified by its Firebase token.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Endpoints;

public static class RegistrationEndpoints
{
    #region Public Methods

    // Maps the registration endpoints onto the given (already authorized) route group
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPost(IdentityRoutes.Register, StartAsync);
        routes.MapPost(IdentityRoutes.RegisterResend, ResendAsync);
        routes.MapPost(IdentityRoutes.RegisterConfirm, ConfirmAsync);
    }

    #endregion

    #region Private Methods

    // Records the account as a Guest and sends the first registration code
    private static async Task<IResult> StartAsync(
        ICurrentUserContext context,
        IRegistrationService registration,
        CancellationToken cancellationToken)
    {
        if (context.UserId is null)
        {
            return Results.Unauthorized();
        }

        var result = await registration.StartAsync(context.UserId, cancellationToken);

        return Results.Json(result, statusCode: IdentityStatusCodes.For(result.Status));
    }

    // Sends a fresh registration code
    private static async Task<IResult> ResendAsync(
        ICurrentUserContext context,
        IRegistrationService registration,
        CancellationToken cancellationToken)
    {
        if (context.UserId is null)
        {
            return Results.Unauthorized();
        }

        var result = await registration.ResendAsync(context.UserId, cancellationToken);

        return Results.Json(result, statusCode: IdentityStatusCodes.For(result.Status));
    }

    // Checks the submitted code and, if valid, completes registration
    private static async Task<IResult> ConfirmAsync(
        ConfirmCodeRequest request,
        ICurrentUserContext context,
        IRegistrationService registration,
        CancellationToken cancellationToken)
    {
        if (context.UserId is null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return Results.BadRequest();
        }

        var status = await registration.ConfirmAsync(context.UserId, request.Code, cancellationToken);

        return Results.Json(
            new StatusResponse<RegistrationStatus>(status),
            statusCode: IdentityStatusCodes.For(status));
    }

    #endregion
}
