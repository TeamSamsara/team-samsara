// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Endpoints/EmailChangeEndpoints.cs
// Version : 1.0.0
// Latest commit: feat/email-change-endpoints
// Author : Gerrah
// Purpose : HTTP endpoints for a member changing their own email: request the two codes, then confirm both.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Shared.Context;
using TeamSamsara.Shared.Http;

namespace TeamSamsara.Modules.Identity.Endpoints;

public static class EmailChangeEndpoints
{
    #region Public Methods

    // Maps the email change endpoints onto the given route group, open to verified members only.
    public static void Map(IEndpointRouteBuilder routes)
    {
        var group = routes
            .MapGroup(IdentityRoutes.EmailGroup)
            .AddEndpointFilter<MemberOnlyEndpointFilter>();

        group.MapPost(IdentityRoutes.EmailRequest, RequestAsync);
        group.MapPost(IdentityRoutes.EmailConfirm, ConfirmAsync);
    }

    #endregion

    #region Private Methods

    // Starts the change and answers identically whether or not the new address is in use.
    private static async Task<IResult> RequestAsync(
        HttpRequest request,
        ICurrentUserContext context,
        IEmailChangeService emailChanges,
        CancellationToken cancellationToken)
    {
        if (context.UserId is null)
        {
            return Results.Unauthorized();
        }

        var body = await request.ReadJsonBodyAsync<ChangeEmailRequest>(cancellationToken);

        if (body is null)
        {
            return Results.BadRequest();
        }

        var result = await emailChanges.RequestChangeAsync(
            context.UserId, body.NewEmail, cancellationToken);

        return Results.Json(result, statusCode: IdentityStatusCodes.For(result.Status));
    }

    // Checks both codes and, if valid, switches the address and signs the member out everywhere.
    private static async Task<IResult> ConfirmAsync(
        HttpRequest request,
        ICurrentUserContext context,
        IEmailChangeService emailChanges,
        CancellationToken cancellationToken)
    {
        if (context.UserId is null)
        {
            return Results.Unauthorized();
        }

        var body = await request.ReadJsonBodyAsync<ConfirmEmailChangeRequest>(cancellationToken);

        if (body is null)
        {
            return Results.BadRequest();
        }

        var status = await emailChanges.ConfirmAsync(
            context.UserId, body.OldCode, body.NewCode, cancellationToken);

        return Results.Json(
            new StatusResponse<EmailChangeStatus>(status),
            statusCode: IdentityStatusCodes.For(status));
    }

    #endregion
}
