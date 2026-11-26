// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/Authentication/NewLoginNotifier.cs
// Version : 1.0.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Default INewLoginNotifier. Sends a courtesy email and leaves a notice in the member's inbox.
// Each is attempted on its own, and a failure of either is logged and never breaks the login.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Shared.Alert;
using TeamSamsara.Shared.Context;
using TeamSamsara.Shared.Notification;

namespace TeamSamsara.Modules.Identity.Handlers;

public class NewLoginNotifier : INewLoginNotifier
{
    #region Fields

    private const string DedupePrefix = "new-login";
    private const string IpAddressDataName = "ipAddress";
    private const string UserAgentDataName = "userAgent";

    // The notification contract allows data values up to 500 characters
    private const int MaxDataValueLength = 500;

    // The fingerprint is base64, which a dedupe key may not contain, so the key carries a hex hash of it instead
    private const int DeviceHashLength = 32;

    private readonly IAlertSender _alerts;
    private readonly INotificationSender _notifications;
    private readonly IClock _clock;
    private readonly ILogger<NewLoginNotifier> _logger;

    #endregion

    #region Constructors

    public NewLoginNotifier(
        IAlertSender alerts,
        INotificationSender notifications,
        IClock clock,
        ILogger<NewLoginNotifier> logger)
    {
        _alerts = alerts;
        _notifications = notifications;
        _clock = clock;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    public async Task NotifyAsync(
        string userId,
        string email,
        ClientInfo client,
        CancellationToken cancellationToken = default)
    {
        await SendEmailAsync(email, client, cancellationToken);
        await LeaveNoticeAsync(userId, client, cancellationToken);
    }

    #endregion

    #region Private Methods

    // The code email already went out, so a failure here is logged and must not break the login
    private async Task SendEmailAsync(
        string email,
        ClientInfo client,
        CancellationToken cancellationToken)
    {
        var templateData = new Dictionary<string, string>
        {
            [AlertTemplateKeys.IpAddress] = client.IpAddress,
            [AlertTemplateKeys.UserAgent] = client.UserAgent
        };

        try
        {
            await _alerts.SendAlertAsync(
                email, AlertType.NewLoginAttempt, templateData, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Could not send the new-login alert.");
        }
    }

    // The member sees the attempt the next time they open the site
    private async Task LeaveNoticeAsync(
        string userId,
        ClientInfo client,
        CancellationToken cancellationToken)
    {
        var request = new NotificationRequest(
            userId, NotificationKeys.NewLogin, BuildData(client), BuildDedupeKey(client));

        try
        {
            var result = await _notifications.SendNotificationAsync(request, cancellationToken);

            if (result.Status is NotificationStatus.Failed or NotificationStatus.Conflict)
            {
                _logger.LogWarning("The new-login notification was not stored ({Status}).", result.Status);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Could not record the new-login notification.");
        }
    }

    private static Dictionary<string, string> BuildData(ClientInfo client)
    {
        return new Dictionary<string, string>
        {
            [IpAddressDataName] = Truncate(client.IpAddress),
            [UserAgentDataName] = Truncate(client.UserAgent)
        };
    }

    // One notice per device per day: the same client on the same UTC day lands on the same notification
    private string BuildDedupeKey(ClientInfo client)
    {
        var device = Convert
            .ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(client.Fingerprint)))[..DeviceHashLength];

        var day = _clock.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        return $"{DedupePrefix}:{device}:{day}";
    }

    // Keeps a value inside what the notification contract accepts
    private static string Truncate(string value)
    {
        return value.Length <= MaxDataValueLength ? value : value[..MaxDataValueLength];
    }

    #endregion
}
