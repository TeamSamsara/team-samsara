// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification.Tests/NotificationSenderTests.cs
// Version : 1.1.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Proves the sender stores each notification once, tells a repeat from a conflicting reuse of a dedupe key, reports storage failures without losing a caller's cancellation or hiding bugs, and keeps each inbox within its limit (oldest read first, then oldest unread, never an unanswered sign-in notice or the notification just stored).

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

    [Fact]
    public async Task Send_WhenTheInboxIsOverItsLimit_RemovesTheOldestReadOne()
    {
        _settings.MaxPerMember = 3;
        var oldestRead = await SeedAsync(Member, minutesAgo: 30, readAt: _clock.UtcNow);
        var unread = await SeedAsync(Member, minutesAgo: 20);
        var newerUnread = await SeedAsync(Member, minutesAgo: 10);

        var result = await CreateSender().SendNotificationAsync(Request());

        result.Status.ShouldBe(NotificationStatus.Stored);
        var ids = _store.Documents.Select(document => document.Id).ToList();
        ids.Count.ShouldBe(3);
        ids.ShouldNotContain(oldestRead.Id);
        ids.ShouldContain(unread.Id);
        ids.ShouldContain(newerUnread.Id);
        ids.ShouldContain(result.Id!);
    }

    [Fact]
    public async Task Send_PrefersAReadOneEvenWhenItIsNewerThanAnUnreadOne()
    {
        _settings.MaxPerMember = 3;
        var oldestUnread = await SeedAsync(Member, minutesAgo: 30);
        var read = await SeedAsync(Member, minutesAgo: 20, readAt: _clock.UtcNow);
        await SeedAsync(Member, minutesAgo: 10);

        await CreateSender().SendNotificationAsync(Request());

        var ids = _store.Documents.Select(document => document.Id).ToList();
        ids.ShouldNotContain(read.Id);
        ids.ShouldContain(oldestUnread.Id);
    }

    [Fact]
    public async Task Send_WhenNothingIsRead_RemovesTheOldestUnreadOne()
    {
        _settings.MaxPerMember = 3;
        var oldest = await SeedAsync(Member, minutesAgo: 30);
        await SeedAsync(Member, minutesAgo: 20);
        await SeedAsync(Member, minutesAgo: 10);

        await CreateSender().SendNotificationAsync(Request());

        _store.Documents.Count.ShouldBe(3);
        _store.Documents.ShouldNotContain(document => document.Id == oldest.Id);
    }

    [Fact]
    public async Task Send_NeverRemovesAnUnansweredSignIn_EvenIfTheInboxStaysOverItsLimit()
    {
        _settings.MaxPerMember = 3;
        await SeedAsync(Member, minutesAgo: 30, key: NotificationKeys.NewLogin);
        await SeedAsync(Member, minutesAgo: 20, key: NotificationKeys.NewLogin);
        await SeedAsync(Member, minutesAgo: 10, key: NotificationKeys.NewLogin);

        var result = await CreateSender().SendNotificationAsync(Request());

        result.Status.ShouldBe(NotificationStatus.Stored);
        _store.Documents.Count.ShouldBe(4);
    }

    [Fact]
    public async Task Send_NeverRemovesTheNotificationItJustStored()
    {
        _settings.MaxPerMember = 1;
        await SeedAsync(Member, minutesAgo: 10, key: NotificationKeys.NewLogin);

        var result = await CreateSender().SendNotificationAsync(Request());

        _store.Documents.ShouldContain(document => document.Id == result.Id);
        _store.DeleteManyCalls.ShouldBe(0);
    }

    [Fact]
    public async Task Send_WithinTheLimit_RemovesNothing()
    {
        _settings.MaxPerMember = 3;
        await SeedAsync(Member, minutesAgo: 20, readAt: _clock.UtcNow);
        await SeedAsync(Member, minutesAgo: 10);

        await CreateSender().SendNotificationAsync(Request());

        _store.Documents.Count.ShouldBe(3);
        _store.DeleteManyCalls.ShouldBe(0);
    }

    [Fact]
    public async Task Send_LeavesAnotherMembersInboxAlone()
    {
        _settings.MaxPerMember = 3;

        for (var minutes = 1; minutes <= 5; minutes++)
        {
            await SeedAsync(OtherMember, minutesAgo: minutes, readAt: _clock.UtcNow);
        }

        await CreateSender().SendNotificationAsync(Request());

        _store.Documents.Count(document => document.RecipientId == OtherMember).ShouldBe(5);
    }

    [Fact]
    public async Task Send_OfADuplicate_DoesNotTrim()
    {
        _settings.MaxPerMember = 3;
        var sender = CreateSender();
        await sender.SendNotificationAsync(Request(dedupeKey: DedupeKey));
        var listCalls = _store.ListCalls;

        var repeat = await sender.SendNotificationAsync(Request(dedupeKey: DedupeKey));

        repeat.Status.ShouldBe(NotificationStatus.Duplicate);
        _store.ListCalls.ShouldBe(listCalls);
        _store.DeleteManyCalls.ShouldBe(0);
    }

    [Fact]
    public async Task Send_WhenStorageFails_DoesNotTrim()
    {
        _store.FailureToThrow = new NotificationStoreException("boom", new InvalidOperationException());

        var result = await CreateSender().SendNotificationAsync(Request());

        result.Status.ShouldBe(NotificationStatus.Failed);
        _store.ListCalls.ShouldBe(0);
        _store.DeleteManyCalls.ShouldBe(0);
    }

    [Fact]
    public async Task Send_WhenTheTrimFails_StillReportsStored()
    {
        _settings.MaxPerMember = 3;
        _store.ListFailureToThrow = new NotificationStoreException("boom", new InvalidOperationException());

        var result = await CreateSender().SendNotificationAsync(Request());

        result.Status.ShouldBe(NotificationStatus.Stored);
        _store.Documents.ShouldHaveSingleItem().Id.ShouldBe(result.Id);
    }

    #endregion

    #region Private Methods

    // Stores a notification that is not part of the test's own sends, with an id the generator never produces
    private async Task<InboxNotification> SeedAsync(
        string recipientId,
        int minutesAgo,
        DateTimeOffset? readAt = null,
        string key = Key)
    {
        var notification = new InboxNotification
        {
            Id = (0xF000 + _store.Documents.Count).ToString("x32"),
            RecipientId = recipientId,
            Key = key,
            Data = Data(OrderNumber),
            CreatedAt = _clock.UtcNow.AddMinutes(-minutesAgo),
            ExpiresAt = _clock.UtcNow.AddDays(DefaultRetentionDays),
            ReadAt = readAt
        };

        (await _store.TryCreateAsync(notification)).ShouldBeTrue();

        return notification;
    }

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
