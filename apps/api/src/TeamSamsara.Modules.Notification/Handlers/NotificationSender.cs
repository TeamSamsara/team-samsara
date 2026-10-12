// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/Handlers/NotificationSender.cs
// Version : 1.0.0
// Latest commit: feat/notification-send-path
// Author : Gerrah
// Purpose : Default INotificationSender. It validates the request and writes it to the inbox, treating a reused dedupe key as a duplicate or a conflict and a storage failure as Failed.

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

    // Validates and stores the notification; storage failures and write timeouts become Failed, a cancelled caller token is rethrown
    public async Task<NotificationResult> SendNotificationAsync(
        NotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        NotificationRequestValidator.Validate(request);

        var notification = Build(request);

        using var writeLimit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        writeLimit.CancelAfter(TimeSpan.FromSeconds(_settings.WriteTimeoutSeconds));

        try
        {
            return await StoreAsync(notification, request.DedupeKey, writeLimit.Token);
        }
        catch (Exception exception) when (exception is NotificationStoreException or OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogError(
                exception,
                "Could not store the notification '{Key}' for member '{RecipientId}' (dedupe key '{DedupeKey}'); it may be lost.",
                request.Key, request.RecipientId, request.DedupeKey);

            return new NotificationResult(NotificationStatus.Failed, null);
        }
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

    // True when both notifications have the same key and the same data
    private static bool HasSameContent(InboxNotification existing, InboxNotification requested)
    {
        return existing.Key == requested.Key
            && existing.Data.Count == requested.Data.Count
            && requested.Data.All(pair => existing.Data.TryGetValue(pair.Key, out var value) && value == pair.Value);
    }

    #endregion
}
