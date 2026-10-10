// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Endpoints/PasswordResetEndpoints.cs
// Version : 1.0.0
// Latest commit: feat/password-reset-endpoints
// Author : Gerrah
// Purpose : HTTP endpoints for a signed-out member who forgot their password: request a code,
// exchange it for a reset token, then set the new password.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Shared.Http;

namespace TeamSamsara.Modules.Identity.Endpoints;

public static class PasswordResetEndpoints
{
    #region Fields

    // Longest valid email address, so oversized input is refused before any work is queued
    private const int MaxEmailLength = 254;

    #endregion

    #region Public Methods

    // Maps the reset endpoints for callers who are not signed in, limited per IP address
    public static void Map(IEndpointRouteBuilder routes)
    {
        var group = routes
            .MapGroup(IdentityRoutes.PasswordResetGroup)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitSettings.PerIpPolicyName);

        group.MapPost(IdentityRoutes.PasswordResetRequest, RequestAsync);
        group.MapPost(IdentityRoutes.PasswordResetVerify, VerifyAsync);
        group.MapPost(IdentityRoutes.PasswordResetConfirm, ConfirmAsync);
    }

    #endregion

    #region Private Methods

    // Queues the reset code email; the answer is the same whether or not the email has an account
    private static async Task<IResult> RequestAsync(
        HttpRequest request,
        IPasswordResetService resets,
        CancellationToken cancellationToken)
    {
        var body = await request.ReadJsonBodyAsync<PasswordResetRequest>(cancellationToken);

        if (body is null || !IsValidEmail(body.Email))
        {
            return Results.BadRequest();
        }

        var result = await resets.RequestResetAsync(body.Email, cancellationToken);

        return Results.Json(result, statusCode: IdentityStatusCodes.For(result.Status));
    }

    // Checks the emailed code and, if valid, answers with a single-use reset token
    private static async Task<IResult> VerifyAsync(
        HttpRequest request,
        IPasswordResetService resets,
        CancellationToken cancellationToken)
    {
        var body = await request.ReadJsonBodyAsync<VerifyResetCodeRequest>(cancellationToken);

        if (body is null || !IsValidEmail(body.Email) || string.IsNullOrWhiteSpace(body.Code))
        {
            return Results.BadRequest();
        }

        var result = await resets.VerifyCodeAsync(body.Email, body.Code, cancellationToken);

        return Results.Json(result, statusCode: IdentityStatusCodes.For(result.Status));
    }

    // Spends the reset token to set the new password and sign the member out everywhere
    private static async Task<IResult> ConfirmAsync(
        HttpRequest request,
        IPasswordResetService resets,
        CancellationToken cancellationToken)
    {
        var body = await request.ReadJsonBodyAsync<SetNewPasswordRequest>(cancellationToken);

        if (body is null
            || string.IsNullOrWhiteSpace(body.ResetToken)
            || string.IsNullOrEmpty(body.NewPassword))
        {
            return Results.BadRequest();
        }

        var status = await resets.SetNewPasswordAsync(
            body.ResetToken, body.NewPassword, cancellationToken);

        return Results.Json(
            new StatusResponse<PasswordStatus>(status),
            statusCode: IdentityStatusCodes.For(status));
    }

    private static bool IsValidEmail(string? email)
    {
        return !string.IsNullOrWhiteSpace(email) && email.Length <= MaxEmailLength;
    }

    #endregion
}
