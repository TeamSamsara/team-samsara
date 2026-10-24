// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Endpoints/EmailChangeEndpoints.cs
// Version : 1.1.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : HTTP endpoints for a member changing their own email: request the two codes, then confirm both.
// The request is limited per IP and per member, since a refused address tells the member it is taken.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Shared.Context;
using TeamSamsara.Shared.Http;

namespace TeamSamsara.Modules.Identity.Endpoints;

public static class EmailChangeEndpoints
{
    #region Fields

    private const int SecondsPerMinute = 60;

    #endregion

    #region Public Methods

    // Maps the email change endpoints onto the given route group, open to verified members only.
    public static void Map(IEndpointRouteBuilder routes)
    {
        var settings = routes.ServiceProvider.GetRequiredService<IOptions<VerificationSettings>>().Value;

        var group = routes
            .MapGroup(IdentityRoutes.EmailGroup)
            .AddEndpointFilter<MemberOnlyEndpointFilter>();

        group.MapPost(IdentityRoutes.EmailRequest, RequestAsync)
            .RequireRateLimiting(RateLimitSettings.PerIpPolicyName)
            .RequirePerMemberRateLimit(
                settings.EmailChangeRequestLimit,
                settings.EmailChangeRequestWindowMinutes * SecondsPerMinute);

        group.MapPost(IdentityRoutes.EmailConfirm, ConfirmAsync);
    }

    #endregion

    #region Private Methods

    // Starts the change, or says straight away why the new address cannot be used.
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

    // Checks the codes sent (one or both) and, once both are accepted, switches the address and signs the member out everywhere.
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
