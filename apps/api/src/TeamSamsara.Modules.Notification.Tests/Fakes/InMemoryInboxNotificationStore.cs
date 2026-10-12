// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification.Tests/Fakes/InMemoryInboxNotificationStore.cs
// Version : 1.0.0
// Latest commit: feat/notification-send-path
// Author : Gerrah
// Purpose : An in-memory inbox store that, like Firestore, refuses to overwrite an existing id, and
// can be made to fail or hang so the sender's error paths can be tested.

using TeamSamsara.Modules.Notification.Models;
using TeamSamsara.Modules.Notification.Repositories;

namespace TeamSamsara.Modules.Notification.Tests.Fakes;

public class InMemoryInboxNotificationStore : IInboxNotificationStore
{
    #region Fields

    private readonly object _lock = new();
    private readonly Dictionary<string, InboxNotification> _documents = new();

    #endregion

    #region Properties

    public Exception? FailureToThrow { get; set; }

    public bool HangUntilCancelled { get; set; }

    public int CreateCalls { get; private set; }

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

    #endregion

    #region Private Methods

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
