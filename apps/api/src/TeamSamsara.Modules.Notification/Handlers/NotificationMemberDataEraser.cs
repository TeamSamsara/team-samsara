// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/Handlers/NotificationMemberDataEraser.cs
// Version : 1.0.0
// Latest commit: feat/notification-erasure
// Author : Gerrah
// Purpose : Removes a member's whole inbox when the account is purged, so no notification outlives the account it belongs to.

using Microsoft.Extensions.Logging;
using TeamSamsara.Modules.Notification.Repositories;
using TeamSamsara.Shared.Members;

namespace TeamSamsara.Modules.Notification.Handlers;

public class NotificationMemberDataEraser : IMemberDataEraser
{
    #region Fields

    private readonly IInboxNotificationStore _store;
    private readonly ILogger<NotificationMemberDataEraser> _logger;

    #endregion

    #region Constructors

    public NotificationMemberDataEraser(IInboxNotificationStore store, ILogger<NotificationMemberDataEraser> logger)
    {
        _store = store;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    // Deletes every notification of the member, one batch at a time until none are left; a storage failure is not caught so the purge keeps the account and retries
    public async Task EraseAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var total = 0;
        int deleted;

        do
        {
            cancellationToken.ThrowIfCancellationRequested();

            deleted = await _store.DeleteBatchByRecipientAsync(userId, cancellationToken);
            total += deleted;
        }
        while (deleted > 0);

        if (total > 0)
        {
            _logger.LogInformation("Erased {Count} notifications of member '{UserId}'.", total, userId);
        }
    }

    #endregion
}
