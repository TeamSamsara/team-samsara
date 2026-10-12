// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification.Tests/NotificationSenderTests.cs
// Version : 1.0.0
// Latest commit: feat/notification-send-path
// Author : Gerrah
// Purpose : Proves the sender stores each notification once, tells a repeat from a conflicting reuse of a dedupe key, and reports storage failures without losing a caller's cancellation or hiding bugs.

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Modules.Notification.Handlers;
using TeamSamsara.Modules.Notification.Models;
using TeamSamsara.Modules.Notification.Repositories;
using TeamSamsara.Modules.Notification.Tests.Fakes;
using TeamSamsara.Shared.Notification;

namespace TeamSamsara.Modules.Notification.Tests;

public class NotificationSenderTests
{
    #region Fields

    private const string Member = "uid-member-1";
    private const string OtherMember = "uid-member-2";
    private const string Key = "order.shipped";
    private const string OtherKey = "order.delivered";
    private const string DedupeKey = "order:1001:shipped";
    private const string OrderNumberName = "orderNumber";
    private const string OrderNumber = "1001";
    private const string OtherOrderNumber = "1002";
    private const int DefaultRetentionDays = 90;
    private const int CustomRetentionDays = 30;
    private const int ZeroTimeoutSeconds = 0;
    private const int RandomIdLength = 32;
    private const int DedupeIdLength = 64;

    private static readonly Regex _lowerHex = new("^[0-9a-f]+\\z", RegexOptions.CultureInvariant);

    private readonly InMemoryInboxNotificationStore _store = new();
    private readonly FakeClock _clock = new();
    private readonly SequentialIdGenerator _ids = new();
    private readonly NotificationSettings _settings = new();

    #endregion

    #region Public Methods

    [Fact]
    public async Task Send_WithoutDedupeKey_StoresOneNotificationWithARandomStyleId()
    {
        var result = await CreateSender().SendNotificationAsync(Request());

        result.Status.ShouldBe(NotificationStatus.Stored);
        result.Id.ShouldNotBeNull();
        result.Id.Length.ShouldBe(RandomIdLength);
        _lowerHex.IsMatch(result.Id).ShouldBeTrue();

        var stored = _store.Documents.ShouldHaveSingleItem();
        stored.Id.ShouldBe(result.Id);
        stored.RecipientId.ShouldBe(Member);
        stored.Key.ShouldBe(Key);
        stored.Data[OrderNumberName].ShouldBe(OrderNumber);
        stored.CreatedAt.ShouldBe(_clock.UtcNow);
        stored.ReadAt.ShouldBeNull();
    }

    [Fact]
    public async Task Send_ExpiresAfterTheDefaultRetention()
    {
        await CreateSender().SendNotificationAsync(Request());

        _store.Documents.ShouldHaveSingleItem().ExpiresAt.ShouldBe(_clock.UtcNow.AddDays(DefaultRetentionDays));
    }

    [Fact]
    public async Task Send_ExpiresAfterTheConfiguredRetention()
    {
        _settings.RetentionDays = CustomRetentionDays;

        await CreateSender().SendNotificationAsync(Request());

        _store.Documents.ShouldHaveSingleItem().ExpiresAt.ShouldBe(_clock.UtcNow.AddDays(CustomRetentionDays));
    }

    [Fact]
    public async Task Send_WithoutData_StoresAnEmptyMap()
    {
        await CreateSender().SendNotificationAsync(new NotificationRequest(Member, Key));

        _store.Documents.ShouldHaveSingleItem().Data.ShouldBeEmpty();
    }

    [Fact]
    public async Task Send_TwiceWithoutDedupeKey_StoresTwoNotifications()
    {
        var sender = CreateSender();

        var first = await sender.SendNotificationAsync(Request());
        var second = await sender.SendNotificationAsync(Request());

        first.Status.ShouldBe(NotificationStatus.Stored);
        second.Status.ShouldBe(NotificationStatus.Stored);
        second.Id.ShouldNotBe(first.Id);
        _store.Documents.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Send_WithDedupeKey_UsesTheDeterministicHashedId()
    {
        var result = await CreateSender().SendNotificationAsync(Request(dedupeKey: DedupeKey));

        result.Status.ShouldBe(NotificationStatus.Stored);
        var id = result.Id.ShouldNotBeNull();
        id.ShouldBe(InboxNotification.BuildDedupeId(Member, DedupeKey));
        id.Length.ShouldBe(DedupeIdLength);
        _lowerHex.IsMatch(id).ShouldBeTrue();
    }

    [Fact]
    public async Task Send_SameRequestTwice_ReportsDuplicateWithTheSameIdAndStoresOnce()
    {
        var sender = CreateSender();

        var first = await sender.SendNotificationAsync(Request(dedupeKey: DedupeKey));
        var second = await sender.SendNotificationAsync(Request(dedupeKey: DedupeKey));

        first.Status.ShouldBe(NotificationStatus.Stored);
        second.Status.ShouldBe(NotificationStatus.Duplicate);
        second.Id.ShouldBe(first.Id);
        _store.Documents.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Send_Duplicate_LeavesTheOriginalUntouched()
    {
        var sender = CreateSender();
        await sender.SendNotificationAsync(Request(dedupeKey: DedupeKey));
        var originalCreatedAt = _clock.UtcNow;
        _clock.Advance(TimeSpan.FromHours(1));

        await sender.SendNotificationAsync(Request(dedupeKey: DedupeKey));

        var stored = _store.Documents.ShouldHaveSingleItem();
        stored.CreatedAt.ShouldBe(originalCreatedAt);
        stored.ReadAt.ShouldBeNull();
    }

    [Fact]
    public async Task Send_SameDedupeKeyWithDifferentData_ReportsConflictAndKeepsTheOriginal()
    {
        var sender = CreateSender();
        await sender.SendNotificationAsync(Request(dedupeKey: DedupeKey));

        var result = await sender.SendNotificationAsync(Request(OtherOrderNumber, dedupeKey: DedupeKey));

        result.Status.ShouldBe(NotificationStatus.Conflict);
        result.Id.ShouldBeNull();
        var stored = _store.Documents.ShouldHaveSingleItem();
        stored.Data[OrderNumberName].ShouldBe(OrderNumber);
    }

    [Fact]
    public async Task Send_SameDedupeKeyWithDifferentKey_ReportsConflictAndKeepsTheOriginal()
    {
        var sender = CreateSender();
        await sender.SendNotificationAsync(Request(dedupeKey: DedupeKey));

        var result = await sender.SendNotificationAsync(
            new NotificationRequest(Member, OtherKey, Data(OrderNumber), DedupeKey));

        result.Status.ShouldBe(NotificationStatus.Conflict);
        _store.Documents.ShouldHaveSingleItem().Key.ShouldBe(Key);
    }

    [Fact]
    public async Task Send_SameDedupeKeyWithExtraDataField_ReportsConflict()
    {
        var sender = CreateSender();
        await sender.SendNotificationAsync(Request(dedupeKey: DedupeKey));

        var richer = new Dictionary<string, string> { [OrderNumberName] = OrderNumber, ["carrier"] = "post" };
        var result = await sender.SendNotificationAsync(new NotificationRequest(Member, Key, richer, DedupeKey));

        result.Status.ShouldBe(NotificationStatus.Conflict);
    }

    [Fact]
    public async Task Send_SameDedupeKeyForTwoMembers_StoresBoth()
    {
        var sender = CreateSender();

        var first = await sender.SendNotificationAsync(Request(dedupeKey: DedupeKey));
        var second = await sender.SendNotificationAsync(
            new NotificationRequest(OtherMember, Key, Data(OrderNumber), DedupeKey));

        first.Status.ShouldBe(NotificationStatus.Stored);
        second.Status.ShouldBe(NotificationStatus.Stored);
        second.Id.ShouldNotBe(first.Id);
        _store.Documents.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Send_WhenTheStoreFails_ReportsFailedWithoutAnId()
    {
        _store.FailureToThrow = new NotificationStoreException("Firestore is down.", new InvalidOperationException());

        var result = await CreateSender().SendNotificationAsync(Request());

        result.Status.ShouldBe(NotificationStatus.Failed);
        result.Id.ShouldBeNull();
        _store.Documents.ShouldBeEmpty();
    }

    [Fact]
    public async Task Send_WhenTheWriteTimesOut_ReportsFailed()
    {
        _settings.WriteTimeoutSeconds = ZeroTimeoutSeconds;
        _store.HangUntilCancelled = true;

        var result = await CreateSender().SendNotificationAsync(Request());

        result.Status.ShouldBe(NotificationStatus.Failed);
        result.Id.ShouldBeNull();
    }

    [Fact]
    public async Task Send_WhenTheCallerCancels_ThrowsInsteadOfReportingFailed()
    {
        _store.HangUntilCancelled = true;
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            () => CreateSender().SendNotificationAsync(Request(), cancellation.Token));

        _store.Documents.ShouldBeEmpty();
    }

    [Fact]
    public async Task Send_WhenSomethingUnexpectedBreaks_LetsTheExceptionThrough()
    {
        _store.FailureToThrow = new InvalidOperationException("A bug.");

        await Should.ThrowAsync<InvalidOperationException>(() => CreateSender().SendNotificationAsync(Request()));
    }

    [Fact]
    public async Task Send_WithAMalformedRequest_ThrowsBeforeTouchingTheStore()
    {
        var malformed = new NotificationRequest(Member, "NoDot");

        await Should.ThrowAsync<ArgumentException>(() => CreateSender().SendNotificationAsync(malformed));

        _store.CreateCalls.ShouldBe(0);
    }

    #endregion

    #region Private Methods

    private NotificationSender CreateSender()
    {
        return new NotificationSender(
            _store,
            _clock,
            _ids,
            Options.Create(_settings),
            NullLogger<NotificationSender>.Instance);
    }

    private static NotificationRequest Request(string orderNumber = OrderNumber, string? dedupeKey = null)
    {
        return new NotificationRequest(Member, Key, Data(orderNumber), dedupeKey);
    }

    private static Dictionary<string, string> Data(string orderNumber)
    {
        return new Dictionary<string, string> { [OrderNumberName] = orderNumber };
    }

    #endregion
}
