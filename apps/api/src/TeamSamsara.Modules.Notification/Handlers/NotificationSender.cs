// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/Handlers/NotificationSender.cs
// Version : 1.1.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Default INotificationSender. It validates the request and writes it to the inbox, treating a reused dedupe key as a duplicate or a conflict and a storage failure as Failed. After a stored notification it trims the member's inbox to its limit.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamSamsara.Modules.Notification.Models;
using TeamSamsara.Modules.Notification.Repositories;
using TeamSamsara.Shared.Context;
using TeamSamsara.Shared.Notification;

namespace TeamSamsara.Modules.Notification.Handlers;

public class NotificationSender : INotificationSender
{
    #region Fields

    private const string IdFormat = "N";

    // How many notifications past the limit one trim looks at, so an inbox that grew before the limit existed shrinks over a few notifications
    private const int TrimWindow = 100;

    private readonly IInboxNotificationStore _store;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;
    private readonly NotificationSettings _settings;
    private readonly ILogger<NotificationSender> _logger;

    #endregion

    #region Constructors

    public NotificationSender(
        IInboxNotificationStore store,
        IClock clock,
        IIdGenerator ids,
        IOptions<NotificationSettings> settings,
        ILogger<NotificationSender> logger)
    {
        _store = store;
        _clock = clock;
        _ids = ids;
        _settings = settings.Value;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    // Validates and stores the notification, then trims the inbox; storage failures and write timeouts become Failed, a failed trim is only logged, and a cancelled caller token is rethrown
    public async Task<NotificationResult> SendNotificationAsync(
        NotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        NotificationRequestValidator.Validate(request);

        var notification = Build(request);
        var result = await WriteAsync(notification, request.DedupeKey, cancellationToken);

        if (result.Status == NotificationStatus.Stored)
        {
            await TrimQuietlyAsync(request.RecipientId, notification.Id, cancellationToken);
        }

        return result;
    }

    #endregion

    #region Private Methods

    // Builds the document to store from the request
    private InboxNotification Build(NotificationRequest request)
    {
        var now = _clock.UtcNow;

        return new InboxNotification
        {
            Id = request.DedupeKey is null
                ? _ids.NewId().ToString(IdFormat)
                : InboxNotification.BuildDedupeId(request.RecipientId, request.DedupeKey),
            RecipientId = request.RecipientId,
            Key = request.Key,
            Data = request.Data is null ? new Dictionary<string, string>() : new Dictionary<string, string>(request.Data),
            CreatedAt = now,
            ExpiresAt = now.AddDays(_settings.RetentionDays)
        };
    }

    // Stores the notification within the write time limit; a storage failure or a timeout becomes Failed
    private async Task<NotificationResult> WriteAsync(
        InboxNotification notification,
        string? dedupeKey,
        CancellationToken cancellationToken)
    {
        using var writeLimit = StartWriteLimit(cancellationToken);

        try
        {
            return await StoreAsync(notification, dedupeKey, writeLimit.Token);
        }
        catch (Exception exception) when (exception is NotificationStoreException or OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogError(
                exception,
                "Could not store the notification '{Key}' for member '{RecipientId}' (dedupe key '{DedupeKey}'); it may be lost.",
                notification.Key, notification.RecipientId, dedupeKey);

            return new NotificationResult(NotificationStatus.Failed, null);
        }
    }

    // A token that is cancelled when the caller cancels or when one storage call has taken too long
    private CancellationTokenSource StartWriteLimit(CancellationToken cancellationToken)
    {
        var writeLimit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        writeLimit.CancelAfter(TimeSpan.FromSeconds(_settings.WriteTimeoutSeconds));

        return writeLimit;
    }

    // Writes the notification and works out what a document already holding its id means
    private async Task<NotificationResult> StoreAsync(
        InboxNotification notification,
        string? dedupeKey,
        CancellationToken cancellationToken)
    {
        if (await _store.TryCreateAsync(notification, cancellationToken) || dedupeKey is null)
        {
            // Without a dedupe key the id is freshly generated, so an existing document is an earlier attempt of this same call
            return new NotificationResult(NotificationStatus.Stored, notification.Id);
        }

        var existing = await _store.GetByIdAsync(notification.Id, cancellationToken);

        if (existing is null)
        {
            _logger.LogWarning(
                "The notification for dedupe key '{DedupeKey}' and member '{RecipientId}' existed and then disappeared while it was checked; reporting it as failed.",
                dedupeKey, notification.RecipientId);

            return new NotificationResult(NotificationStatus.Failed, null);
        }

        if (HasSameContent(existing, notification))
        {
            return new NotificationResult(NotificationStatus.Duplicate, existing.Id);
        }

        _logger.LogError(
            "Dedupe key '{DedupeKey}' for member '{RecipientId}' was reused for a different notification ('{Key}'); nothing was stored.",
            dedupeKey, notification.RecipientId, notification.Key);

        return new NotificationResult(NotificationStatus.Conflict, null);
    }

    // Keeps the inbox within its limit; the notification that was just stored is never removed, and a failure is logged because the notification itself was stored
    private async Task TrimQuietlyAsync(string recipientId, string storedId, CancellationToken cancellationToken)
    {
        using var writeLimit = StartWriteLimit(cancellationToken);

        try
        {
            var newest = await _store.ListByRecipientAsync(
                recipientId, _settings.MaxPerMember + TrimWindow, null, writeLimit.Token);

            var excess = InboxRetention.SelectForTrim(newest, _settings.MaxPerMember)
                .Where(notification => notification.Id != storedId)
                .ToList();

            if (excess.Count > 0)
            {
                await _store.DeleteManyAsync(excess, writeLimit.Token);
            }
        }
        catch (Exception exception) when (exception is NotificationStoreException or OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogWarning(
                exception,
                "Could not trim the inbox of member '{RecipientId}'; it stays over its limit until the next notification.",
                recipientId);
        }
    }

    // True when both notifications have the same key and the same data
    private static bool HasSameContent(InboxNotification existing, InboxNotification requested)
    {
        return existing.Key == requested.Key
            && existing.Data.Count == requested.Data.Count
            && requested.Data.All(pair => existing.Data.TryGetValue(pair.Key, out var value) && value == pair.Value);
    }

    #endregion
}
