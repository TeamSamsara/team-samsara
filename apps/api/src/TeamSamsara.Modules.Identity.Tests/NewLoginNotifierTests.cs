// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/NewLoginNotifierTests.cs
// Version : 1.0.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Proves the new-login notice: the email and the inbox notice both go out, the notice
// is one per device per day, long values are cut to what the contract accepts, and a failure of
// either part never stops the other or reaches the caller.

using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Tests.Fakes;
using TeamSamsara.Shared.Alert;
using TeamSamsara.Shared.Notification;

namespace TeamSamsara.Modules.Identity.Tests;

public class NewLoginNotifierTests
{
    #region Fields

    private const string UserId = "user-1";
    private const string Email = "member@example.com";
    private const int MaxDataValueLength = 500;

    private static readonly ClientInfo _client = new("198.51.100.9", "OtherBrowser", "fingerprint-b");
    private static readonly ClientInfo _otherClient = new("192.0.2.1", "ThirdBrowser", "fingerprint-c");

    private readonly RecordingAlertSender _alerts = new();
    private readonly RecordingNotificationSender _notifications = new();
    private readonly FakeClock _clock = new();
    private readonly NewLoginNotifier _notifier;

    #endregion

    #region Constructors

    public NewLoginNotifierTests()
    {
        _notifier = new NewLoginNotifier(_alerts, _notifications, _clock, NullLogger<NewLoginNotifier>.Instance);
    }

    #endregion

    #region Public Methods

    [Fact]
    public async Task Notify_SendsTheEmail_AndLeavesTheInboxNotice()
    {
        await _notifier.NotifyAsync(UserId, Email, _client);

        var alert = _alerts.Sent.ShouldHaveSingleItem();
        alert.Type.ShouldBe(AlertType.NewLoginAttempt);
        alert.To.ShouldBe(Email);
        alert.TemplateData[AlertTemplateKeys.IpAddress].ShouldBe(_client.IpAddress);
        alert.TemplateData[AlertTemplateKeys.UserAgent].ShouldBe(_client.UserAgent);

        var request = _notifications.Requests.ShouldHaveSingleItem();
        request.RecipientId.ShouldBe(UserId);
        request.Key.ShouldBe(NotificationKeys.NewLogin);
        request.Data.ShouldNotBeNull();
        request.Data["ipAddress"].ShouldBe(_client.IpAddress);
        request.Data["userAgent"].ShouldBe(_client.UserAgent);
    }

    [Fact]
    public async Task Notify_ForTheSameClientOnTheSameDay_UsesTheSameDedupeKey()
    {
        await _notifier.NotifyAsync(UserId, Email, _client);
        _clock.Advance(TimeSpan.FromHours(1));
        await _notifier.NotifyAsync(UserId, Email, _client);

        _notifications.Requests[1].DedupeKey.ShouldBe(_notifications.Requests[0].DedupeKey);
    }

    [Fact]
    public async Task Notify_ForAnotherClient_UsesAnotherDedupeKey()
    {
        await _notifier.NotifyAsync(UserId, Email, _client);
        await _notifier.NotifyAsync(UserId, Email, _otherClient);

        _notifications.Requests[1].DedupeKey.ShouldNotBe(_notifications.Requests[0].DedupeKey);
    }

    [Fact]
    public async Task Notify_ForTheSameClientOnTheNextDay_UsesAnotherDedupeKey()
    {
        await _notifier.NotifyAsync(UserId, Email, _client);
        _clock.Advance(TimeSpan.FromDays(1));
        await _notifier.NotifyAsync(UserId, Email, _client);

        _notifications.Requests[1].DedupeKey.ShouldNotBe(_notifications.Requests[0].DedupeKey);
    }

    [Fact]
    public async Task Notify_NeverPutsTheRawFingerprintInTheDedupeKey()
    {
        await _notifier.NotifyAsync(UserId, Email, _client);

        _notifications.Requests.ShouldHaveSingleItem().DedupeKey.ShouldNotBeNull().ShouldNotContain(_client.Fingerprint);
    }

    [Fact]
    public async Task Notify_CutsLongValuesToWhatTheContractAccepts()
    {
        var client = new ClientInfo(_client.IpAddress, new string('x', MaxDataValueLength + 50), _client.Fingerprint);

        await _notifier.NotifyAsync(UserId, Email, client);

        var data = _notifications.Requests.ShouldHaveSingleItem().Data;
        data.ShouldNotBeNull();
        data["userAgent"].Length.ShouldBe(MaxDataValueLength);
    }

    [Fact]
    public async Task Notify_WhenTheEmailFails_StillLeavesTheInboxNotice()
    {
        _alerts.FailingTypes.Add(AlertType.NewLoginAttempt);

        await _notifier.NotifyAsync(UserId, Email, _client);

        _notifications.Requests.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Notify_WhenTheInboxThrows_StillSendsTheEmail_AndDoesNotThrow()
    {
        _notifications.FailureToThrow = new InvalidOperationException("Simulated inbox failure.");

        await _notifier.NotifyAsync(UserId, Email, _client);

        _alerts.Sent.ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData(NotificationStatus.Failed)]
    [InlineData(NotificationStatus.Conflict)]
    [InlineData(NotificationStatus.Duplicate)]
    public async Task Notify_WhenTheInboxReportsAnythingButStored_DoesNotThrow(NotificationStatus status)
    {
        _notifications.StatusToReturn = status;

        await Should.NotThrowAsync(() => _notifier.NotifyAsync(UserId, Email, _client));
    }

    [Fact]
    public async Task Notify_WhenCancelled_Throws()
    {
        _notifications.FailureToThrow = new OperationCanceledException();

        await Should.ThrowAsync<OperationCanceledException>(() => _notifier.NotifyAsync(UserId, Email, _client));
    }

    #endregion
}
