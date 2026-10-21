// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification.Tests/Fakes/InMemoryInboxNotificationStore.cs
// Version : 1.2.0
// Latest commit: task/notifications-api
// Author : Gerrah
// Purpose : An in-memory inbox store that, like Firestore, refuses to overwrite an existing id, orders and pages like the real queries, and can be made to fail or hang so the error paths can be tested.

using TeamSamsara.Modules.Notification.Models;
using TeamSamsara.Modules.Notification.Repositories;

namespace TeamSamsara.Modules.Notification.Tests.Fakes;

public class InMemoryInboxNotificationStore : IInboxNotificationStore
{
    #region Fields

    private const int DefaultBatchSize = 500;

    private readonly object _lock = new();
    private readonly Dictionary<string, InboxNotification> _documents = new();

    #endregion

    #region Properties

    public Exception? FailureToThrow { get; set; }

    public bool HangUntilCancelled { get; set; }

    public int DeleteBatchSize { get; set; } = DefaultBatchSize;

    public int MarkBatchSize { get; set; } = DefaultBatchSize;

    public int CreateCalls { get; private set; }

    public int DeleteBatchCalls { get; private set; }

    public int ListCalls { get; private set; }

    public int MarkReadCalls { get; private set; }

    public int MarkBatchCalls { get; private set; }

    public IReadOnlyList<InboxNotification> Documents
    {
        get
        {
            lock (_lock)
            {
                return _documents.Values.Select(Copy).ToList();
            }
        }
    }

    #endregion

    #region Public Methods

    public async Task<bool> TryCreateAsync(InboxNotification notification, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            CreateCalls++;
        }

        if (HangUntilCancelled)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        if (FailureToThrow is not null)
        {
            throw FailureToThrow;
        }

        lock (_lock)
        {
            return _documents.TryAdd(notification.Id, Copy(notification));
        }
    }

    public Task<InboxNotification?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (FailureToThrow is not null)
        {
            throw FailureToThrow;
        }

        lock (_lock)
        {
            return Task.FromResult(_documents.TryGetValue(id, out var document) ? Copy(document) : null);
        }
    }

    // Newest first, ties by id descending, like the Firestore query
    public Task<IReadOnlyList<InboxNotification>> ListByRecipientAsync(
        string recipientId,
        int limit,
        InboxCursor? after,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            ListCalls++;
        }

        ThrowIfFailing();

        lock (_lock)
        {
            IReadOnlyList<InboxNotification> page = _documents.Values
                .Where(document => document.RecipientId == recipientId)
                .OrderByDescending(document => document.CreatedAt)
                .ThenByDescending(document => document.Id, StringComparer.Ordinal)
                .Where(document => after is null || IsAfter(document, after))
                .Take(limit)
                .Select(Copy)
                .ToList();

            return Task.FromResult(page);
        }
    }

    public Task<int> CountUnreadByRecipientAsync(string recipientId, CancellationToken cancellationToken = default)
    {
        ThrowIfFailing();

        lock (_lock)
        {
            return Task.FromResult(_documents.Values.Count(
                document => document.RecipientId == recipientId && document.ReadAt is null));
        }
    }

    public Task<bool> MarkReadAsync(
        string recipientId,
        string id,
        DateTimeOffset readAt,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            MarkReadCalls++;
        }

        ThrowIfFailing();

        lock (_lock)
        {
            if (!_documents.TryGetValue(id, out var document) || document.RecipientId != recipientId)
            {
                return Task.FromResult(false);
            }

            document.ReadAt ??= readAt;

            return Task.FromResult(true);
        }
    }

    public Task<int> MarkBatchReadAsync(
        string recipientId,
        DateTimeOffset readAt,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            MarkBatchCalls++;
        }

        ThrowIfFailing();

        lock (_lock)
        {
            var unread = _documents.Values
                .Where(document => document.RecipientId == recipientId && document.ReadAt is null)
                .Take(MarkBatchSize)
                .ToList();

            foreach (var document in unread)
            {
                document.ReadAt = readAt;
            }

            return Task.FromResult(unread.Count);
        }
    }

    public Task<int> DeleteBatchByRecipientAsync(string recipientId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            DeleteBatchCalls++;
        }

        if (FailureToThrow is not null)
        {
            throw FailureToThrow;
        }

        lock (_lock)
        {
            var ids = _documents.Values
                .Where(document => document.RecipientId == recipientId)
                .Take(DeleteBatchSize)
                .Select(document => document.Id)
                .ToList();

            foreach (var id in ids)
            {
                _documents.Remove(id);
            }

            return Task.FromResult(ids.Count);
        }
    }

    #endregion

    #region Private Methods

    private void ThrowIfFailing()
    {
        if (FailureToThrow is not null)
        {
            throw FailureToThrow;
        }
    }

    // True when the document comes after the cursor in the newest-first order
    private static bool IsAfter(InboxNotification document, InboxCursor cursor)
    {
        return document.CreatedAt < cursor.CreatedAt
            || (document.CreatedAt == cursor.CreatedAt && string.CompareOrdinal(document.Id, cursor.Id) < 0);
    }

    private static InboxNotification Copy(InboxNotification source)
    {
        return new InboxNotification
        {
            Id = source.Id,
            RecipientId = source.RecipientId,
            Key = source.Key,
            Data = new Dictionary<string, string>(source.Data),
            CreatedAt = source.CreatedAt,
            ExpiresAt = source.ExpiresAt,
            ReadAt = source.ReadAt,
        };
    }

    #endregion
}
