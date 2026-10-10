// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Endpoints/AccountDeletionEndpoints.cs
// Version : 1.0.0
// Latest commit: feat/account-deletion
// Author : Gerrah
// Purpose : HTTP endpoints for a member deleting their own account: request the confirmation
// code, then submit it to delete.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Shared.Context;
using TeamSamsara.Shared.Http;

namespace TeamSamsara.Modules.Identity.Endpoints;

public static class AccountDeletionEndpoints
{
    #region Public Methods

    // Maps the account deletion endpoints onto the given route group, open to verified members only
    public static void Map(IEndpointRouteBuilder routes)
    {
        var group = routes
            .MapGroup(IdentityRoutes.DeleteGroup)
            .AddEndpointFilter<MemberOnlyEndpointFilter>();

        group.MapPost(IdentityRoutes.DeleteRequestCode, RequestCodeAsync);
        group.MapPost(IdentityRoutes.DeleteConfirm, DeleteAsync);
    }

    #endregion

    #region Private Methods

    // Emails the member a confirmation code
    private static async Task<IResult> RequestCodeAsync(
        ICurrentUserContext context,
        IAccountDeletionService deletion,
        CancellationToken cancellationToken)
    {
        if (context.UserId is null)
        {
            return Results.Unauthorized();
        }

        var result = await deletion.RequestDeletionCodeAsync(context.UserId, cancellationToken);

        return Results.Json(result, statusCode: IdentityStatusCodes.For(result.Status));
    }

    // Checks the code and, if valid, deletes the account and signs the member out everywhere
    private static async Task<IResult> DeleteAsync(
        HttpRequest request,
        ICurrentUserContext context,
        IAccountDeletionService deletion,
        CancellationToken cancellationToken)
    {
        if (context.UserId is null)
        {
            return Results.Unauthorized();
        }

        var body = await request.ReadJsonBodyAsync<DeleteAccountRequest>(cancellationToken);

        if (body is null || string.IsNullOrWhiteSpace(body.Code))
        {
            return Results.BadRequest();
        }

        var status = await deletion.DeleteAsync(context.UserId, body.Code, cancellationToken);

        return Results.Json(
            new StatusResponse<AccountDeletionStatus>(status),
            statusCode: IdentityStatusCodes.For(status));
    }

    #endregion
}
