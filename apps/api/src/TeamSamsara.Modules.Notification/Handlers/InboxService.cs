// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/Handlers/InboxService.cs
// Version : 1.1.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Default IInboxService. Pages the inbox with a cursor, hides expired notifications the TTL has not deleted yet, refuses malformed cursors and ids before they reach storage, and deletes notifications except unanswered sign-in notices.

using System.Diagnostics.CodeAnalysis;
using TeamSamsara.Modules.Notification.Models;
using TeamSamsara.Modules.Notification.Repositories;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Notification.Handlers;

public class InboxService : IInboxService
{
    #region Fields

    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;

    // Stops "mark all as read" and "clear read" from looping forever; 20 batches cover 10,000 notifications
    private const int MaxBatches = 20;

    private readonly IInboxNotificationStore _store;
    private readonly IClock _clock;

    #endregion

    #region Constructors

    public InboxService(IInboxNotificationStore store, IClock clock)
    {
        _store = store;
        _clock = clock;
    }

    #endregion

    #region Public Methods

    // Fetches one more than the page size to learn whether another page exists, then hides the expired ones
    public async Task<NotificationPage?> ListAsync(
        string recipientId,
        int? limit,
        string? cursor,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientId);

        if (!TryReadCursor(cursor, out var after))
        {
            return null;
        }

        var pageSize = Math.Clamp(limit ?? DefaultPageSize, 1, MaxPageSize);
        var fetched = await _store.ListByRecipientAsync(recipientId, pageSize + 1, after, cancellationToken);

        var pageItems = fetched.Take(pageSize).ToList();
        var now = _clock.UtcNow;

        var items = pageItems
            .Where(notification => notification.ExpiresAt > now)
            .Select(ToSummary)
            .ToList();

        var nextCursor = fetched.Count > pageSize ? ToCursor(pageItems[^1]) : null;

        return new NotificationPage(items, nextCursor);
    }

    public async Task<int> CountUnreadAsync(string recipientId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientId);

        return await _store.CountUnreadByRecipientAsync(recipientId, cancellationToken);
    }

    public async Task<bool> MarkReadAsync(string recipientId, string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientId);

        if (!NotificationIds.IsValid(id))
        {
            return false;
        }

        return await _store.MarkReadAsync(recipientId, id, _clock.UtcNow, cancellationToken);
    }

    public async Task<int> MarkAllReadAsync(string recipientId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientId);

        return await RunBatchesAsync(
            () => _store.MarkBatchReadAsync(recipientId, _clock.UtcNow, cancellationToken),
            cancellationToken);
    }

    // The store decides on the stored notification, so an unanswered sign-in notice cannot slip through between a check and the deletion
    public async Task<NotificationDeleteStatus> DeleteAsync(
        string recipientId,
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientId);

        if (!NotificationIds.IsValid(id))
        {
            return NotificationDeleteStatus.NotFound;
        }

        return await _store.DeleteAsync(recipientId, id, InboxRetention.IsClearable, cancellationToken);
    }

    public async Task<int> ClearReadAsync(string recipientId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientId);

        return await RunBatchesAsync(
            () => _store.DeleteReadBatchAsync(recipientId, cancellationToken),
            cancellationToken);
    }

    #endregion

    #region Private Methods

    // Runs one batch after another until none are left, or until the safety limit is reached, and returns how many were handled in all
    private static async Task<int> RunBatchesAsync(Func<Task<int>> runBatch, CancellationToken cancellationToken)
    {
        var total = 0;

        for (var batch = 0; batch < MaxBatches; batch++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var handled = await runBatch();

            if (handled == 0)
            {
                break;
            }

            total += handled;
        }

        return total;
    }

    // No cursor (or an empty one) means the first page; anything else must be a cursor we produced
    private static bool TryReadCursor(string? cursor, out InboxCursor? after)
    {
        after = null;

        if (string.IsNullOrEmpty(cursor))
        {
            return true;
        }

        return InboxCursor.TryParse(cursor, out after);
    }

    // The cursor for the page that follows this one: it starts just after the given notification
    private static string ToCursor([NotNull] InboxNotification last)
    {
        return new InboxCursor(last.CreatedAt, last.Id).Encode();
    }

    private static NotificationSummary ToSummary(InboxNotification notification)
    {
        return new NotificationSummary(
            notification.Id,
            notification.Key,
            notification.Data,
            notification.CreatedAt,
            notification.ReadAt);
    }

    #endregion
}
