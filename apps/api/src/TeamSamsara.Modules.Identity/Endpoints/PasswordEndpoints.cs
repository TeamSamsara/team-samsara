// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Endpoints/PasswordEndpoints.cs
// Version : 1.1.0
// Latest commit: feat/password-reset-endpoints
// Author : Gerrah
// Purpose : HTTP endpoints for a member changing their own password: request the confirmation
// code, then submit the new password with that code.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Shared.Context;
using TeamSamsara.Shared.Http;

namespace TeamSamsara.Modules.Identity.Endpoints;

public static class PasswordEndpoints
{
    #region Public Methods

    // Maps the password endpoints onto the given route group, open to verified members only
    public static void Map(IEndpointRouteBuilder routes)
    {
        var group = routes
            .MapGroup(IdentityRoutes.PasswordGroup)
            .AddEndpointFilter<MemberOnlyEndpointFilter>();

        group.MapPost(IdentityRoutes.PasswordRequestCode, RequestCodeAsync);
        group.MapPost(IdentityRoutes.PasswordChange, ChangeAsync);
    }

    #endregion

    #region Private Methods

    // Checks the wanted password against the rules and emails the member a confirmation code
    private static async Task<IResult> RequestCodeAsync(
        HttpRequest request,
        ICurrentUserContext context,
        IPasswordService passwords,
        CancellationToken cancellationToken)
    {
        if (context.UserId is null)
        {
            return Results.Unauthorized();
        }

        var body = await request.ReadJsonBodyAsync<PasswordCodeRequest>(cancellationToken);

        if (body is null || string.IsNullOrEmpty(body.NewPassword))
        {
            return Results.BadRequest();
        }

        var result = await passwords.RequestChangeCodeAsync(
            context.UserId, body.NewPassword, cancellationToken);

        return Results.Json(result, statusCode: IdentityStatusCodes.For(result.Status));
    }

    // Checks the code and, if valid, replaces the password and signs the member out everywhere
    private static async Task<IResult> ChangeAsync(
        HttpRequest request,
        ICurrentUserContext context,
        IPasswordService passwords,
        CancellationToken cancellationToken)
    {
        if (context.UserId is null)
        {
            return Results.Unauthorized();
        }

        var body = await request.ReadJsonBodyAsync<ChangePasswordRequest>(cancellationToken);

        if (body is null
            || string.IsNullOrEmpty(body.NewPassword)
            || string.IsNullOrWhiteSpace(body.Code))
        {
            return Results.BadRequest();
        }

        var status = await passwords.ChangeAsync(
            context.UserId, body.NewPassword, body.Code, cancellationToken);

        return Results.Json(
            new StatusResponse<PasswordStatus>(status),
            statusCode: IdentityStatusCodes.For(status));
    }

    #endregion
}
