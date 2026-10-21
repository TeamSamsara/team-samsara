// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/Endpoints/NotificationEndpoints.cs
// Version : 1.0.0
// Latest commit: task/notifications-api
// Author : Gerrah
// Purpose : HTTP endpoints for a verified member's own inbox: list a page, count the unread, mark one or all as read.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TeamSamsara.Modules.Notification.Handlers;
using TeamSamsara.Modules.Notification.Models;
using TeamSamsara.Shared.Context;
using TeamSamsara.Shared.Http;

namespace TeamSamsara.Modules.Notification.Endpoints;

public static class NotificationEndpoints
{
    #region Public Methods

    // Maps the inbox endpoints onto the given route group, open to verified members only; every call reads or changes the caller's own notifications.
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet(NotificationRoutes.Root, ListAsync)
            .AddEndpointFilter<MemberOnlyEndpointFilter>()
            .Produces<NotificationPage>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        routes.MapGet(NotificationRoutes.UnreadCount, CountUnreadAsync)
            .AddEndpointFilter<MemberOnlyEndpointFilter>()
            .Produces<UnreadCountResponse>(StatusCodes.Status200OK);

        routes.MapPost(NotificationRoutes.ReadAll, MarkAllReadAsync)
            .AddEndpointFilter<MemberOnlyEndpointFilter>()
            .Produces(StatusCodes.Status204NoContent);

        routes.MapPost(NotificationRoutes.Read, MarkReadAsync)
            .AddEndpointFilter<MemberOnlyEndpointFilter>()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    #endregion

    #region Private Methods

    // Returns one page of the caller's notifications; a malformed cursor is a bad request
    private static async Task<IResult> ListAsync(
        int? limit,
        string? cursor,
        ICurrentUserContext context,
        IInboxService inbox,
        CancellationToken cancellationToken)
    {
        if (context.UserId is null)
        {
            return Results.Unauthorized();
        }

        var page = await inbox.ListAsync(context.UserId, limit, cursor, cancellationToken);

        return page is null ? Results.BadRequest() : Results.Ok(page);
    }

    // Returns how many of the caller's notifications are unread
    private static async Task<IResult> CountUnreadAsync(
        ICurrentUserContext context,
        IInboxService inbox,
        CancellationToken cancellationToken)
    {
        if (context.UserId is null)
        {
            return Results.Unauthorized();
        }

        var count = await inbox.CountUnreadAsync(context.UserId, cancellationToken);

        return Results.Ok(new UnreadCountResponse(count));
    }

    // Marks every unread notification of the caller as read
    private static async Task<IResult> MarkAllReadAsync(
        ICurrentUserContext context,
        IInboxService inbox,
        CancellationToken cancellationToken)
    {
        if (context.UserId is null)
        {
            return Results.Unauthorized();
        }

        await inbox.MarkAllReadAsync(context.UserId, cancellationToken);

        return Results.NoContent();
    }

    // Marks one notification as read; an unknown id and someone else's both answer not found, so ids cannot be probed
    private static async Task<IResult> MarkReadAsync(
        string id,
        ICurrentUserContext context,
        IInboxService inbox,
        CancellationToken cancellationToken)
    {
        if (context.UserId is null)
        {
            return Results.Unauthorized();
        }

        var found = await inbox.MarkReadAsync(context.UserId, id, cancellationToken);

        return found ? Results.NoContent() : Results.NotFound();
    }

    #endregion
}
