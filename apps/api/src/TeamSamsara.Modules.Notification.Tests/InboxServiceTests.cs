// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification.Tests/InboxServiceTests.cs
// Version : 1.0.0
// Latest commit: task/notifications-api
// Author : Gerrah
// Purpose : Proves the inbox service pages a member's notifications newest first without repeats or gaps, hides the expired ones, counts what is unread, marks one or all as read for the owner only, and refuses malformed cursors and ids before storage sees them.

using Shouldly;
using TeamSamsara.Modules.Notification.Handlers;
using TeamSamsara.Modules.Notification.Models;
using TeamSamsara.Modules.Notification.Repositories;
using TeamSamsara.Modules.Notification.Tests.Fakes;

namespace TeamSamsara.Modules.Notification.Tests;

public class InboxServiceTests
{
    #region Fields

    private const string Member = "uid-member-1";
    private const string OtherMember = "uid-member-2";
    private const string Key = "order.shipped";
    private const string OrderNumberName = "orderNumber";
    private const string OrderNumber = "1001";
    private const int RetentionDays = 90;

    // Mirrors the service's safety limit on how many batches "mark all as read" works through
    private const int MarkAllBatchLimit = 20;

    private readonly InMemoryInboxNotificationStore _store = new();
    private readonly FakeClock _clock = new();
    private readonly InboxService _service;
    private int _lastId;

    #endregion

    #region Constructors

    public InboxServiceTests()
    {
        _service = new InboxService(_store, _clock);
    }

    #endregion

    #region Public Methods

    [Fact]
    public async Task List_ReturnsOnlyTheMembersNotifications_NewestFirst()
    {
        var older = await SeedAsync(Member, minutesAgo: 10);
        var newer = await SeedAsync(Member, minutesAgo: 5);
        await SeedAsync(OtherMember, minutesAgo: 1);

        var page = await _service.ListAsync(Member, null, null);

        page.ShouldNotBeNull();
        page.Items.Select(item => item.Id).ShouldBe(new[] { newer.Id, older.Id });
        page.NextCursor.ShouldBeNull();
    }

    [Fact]
    public async Task List_ShowsWhatHappenedAndWhen()
    {
        var readAt = _clock.UtcNow.AddMinutes(-1);
        var stored = await SeedAsync(Member, minutesAgo: 5, readAt: readAt);

        var page = await _service.ListAsync(Member, null, null);

        page.ShouldNotBeNull();
        var item = page.Items.ShouldHaveSingleItem();
        item.Id.ShouldBe(stored.Id);
        item.Key.ShouldBe(Key);
        item.Data[OrderNumberName].ShouldBe(OrderNumber);
        item.CreatedAt.ShouldBe(stored.CreatedAt);
        item.ReadAt.ShouldBe(readAt);
    }

    [Fact]
    public async Task List_ForAMemberWithNothing_ReturnsAnEmptyPage()
    {
        await SeedAsync(OtherMember, minutesAgo: 1);

        var page = await _service.ListAsync(Member, null, null);

        page.ShouldNotBeNull();
        page.Items.ShouldBeEmpty();
        page.NextCursor.ShouldBeNull();
    }

    [Fact]
    public async Task List_WithTheCursor_WalksEveryNotificationOnceInOrder()
    {
        var created = new List<InboxNotification>();

        for (var minutesAgo = 5; minutesAgo >= 1; minutesAgo--)
        {
            created.Add(await SeedAsync(Member, minutesAgo));
        }

        var (ids, pages) = await ReadAllPagesAsync(limit: 2);

        ids.ShouldBe(created.AsEnumerable().Reverse().Select(notification => notification.Id));
        pages.ShouldBe(3);
    }

    [Fact]
    public async Task List_WhenTheLastPageIsExactlyFull_OffersNoCursor()
    {
        for (var minutesAgo = 4; minutesAgo >= 1; minutesAgo--)
        {
            await SeedAsync(Member, minutesAgo);
        }

        var first = await _service.ListAsync(Member, 2, null);
        first.ShouldNotBeNull();
        first.NextCursor.ShouldNotBeNull();

        var second = await _service.ListAsync(Member, 2, first.NextCursor);

        second.ShouldNotBeNull();
        second.Items.Count.ShouldBe(2);
        second.NextCursor.ShouldBeNull();
    }

    [Fact]
    public async Task List_WithTheSameTime_OrdersByIdDescending_AndPagesWithoutRepeatsOrGaps()
    {
        var first = await SeedAsync(Member, minutesAgo: 3);
        var second = await SeedAsync(Member, minutesAgo: 3);

        var (ids, pages) = await ReadAllPagesAsync(limit: 1);

        ids.ShouldBe(new[] { second.Id, first.Id });
        pages.ShouldBe(2);
    }

    [Theory]
    [InlineData(null, InboxService.DefaultPageSize)]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(7, 7)]
    [InlineData(1000, InboxService.MaxPageSize)]
    public async Task List_UsesTheDefaultPageSize_AndClampsTheLimit(int? limit, int expected)
    {
        for (var minutesAgo = 1; minutesAgo <= 60; minutesAgo++)
        {
            await SeedAsync(Member, minutesAgo);
        }

        var page = await _service.ListAsync(Member, limit, null);

        page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(expected);
    }

    [Fact]
    public async Task List_HidesExpiredNotifications_IncludingOneThatExpiresRightNow()
    {
        var live = await SeedAsync(Member, minutesAgo: 1);
        await SeedAsync(Member, minutesAgo: 2, expiresAt: _clock.UtcNow);
        await SeedAsync(Member, minutesAgo: 3, expiresAt: _clock.UtcNow.AddMinutes(-1));

        var page = await _service.ListAsync(Member, null, null);

        page.ShouldNotBeNull();
        page.Items.ShouldHaveSingleItem().Id.ShouldBe(live.Id);
    }

    [Fact]
    public async Task List_WhenAnExpiredNotificationFillsAPage_StillOffersTheNextPage()
    {
        await SeedAsync(Member, minutesAgo: 1, expiresAt: _clock.UtcNow.AddMinutes(-1));
        var live = await SeedAsync(Member, minutesAgo: 2);

        var first = await _service.ListAsync(Member, 1, null);

        first.ShouldNotBeNull();
        first.Items.ShouldBeEmpty();
        first.NextCursor.ShouldNotBeNull();

        var second = await _service.ListAsync(Member, 1, first.NextCursor);

        second.ShouldNotBeNull();
        second.Items.ShouldHaveSingleItem().Id.ShouldBe(live.Id);
    }

    [Fact]
    public async Task List_WithACursorFromAnotherMembersInbox_OnlyUsesItAsAPosition()
    {
        await SeedAsync(Member, minutesAgo: 10);
        var foreign = await SeedAsync(OtherMember, minutesAgo: 5);
        var cursor = new InboxCursor(foreign.CreatedAt, foreign.Id).Encode();

        var page = await _service.ListAsync(Member, null, cursor);

        page.ShouldNotBeNull();
        page.Items.ShouldHaveSingleItem().Id.ShouldNotBe(foreign.Id);
        page.Items.ShouldAllBe(item => _store.Documents.Single(document => document.Id == item.Id).RecipientId == Member);
    }

    [Theory]
    [InlineData("garbage")]
    [InlineData("v2.abc")]
    [InlineData("v1.!!!")]
    public async Task List_WithAMalformedCursor_ReturnsNull_WithoutReadingStorage(string cursor)
    {
        await SeedAsync(Member, minutesAgo: 1);

        var page = await _service.ListAsync(Member, null, cursor);

        page.ShouldBeNull();
        _store.ListCalls.ShouldBe(0);
    }

    [Fact]
    public async Task List_WithAnEmptyCursor_ReturnsTheFirstPage()
    {
        var stored = await SeedAsync(Member, minutesAgo: 1);

        var page = await _service.ListAsync(Member, null, string.Empty);

        page.ShouldNotBeNull();
        page.Items.ShouldHaveSingleItem().Id.ShouldBe(stored.Id);
    }

    [Fact]
    public async Task List_ForABlankMember_Throws()
    {
        await Should.ThrowAsync<ArgumentException>(async () => await _service.ListAsync("  ", null, null));
    }

    [Fact]
    public async Task List_WhenStorageFails_Throws()
    {
        _store.FailureToThrow = new NotificationStoreException("boom", new InvalidOperationException());

        await Should.ThrowAsync<NotificationStoreException>(async () => await _service.ListAsync(Member, null, null));
    }

    [Fact]
    public async Task CountUnread_CountsOnlyTheMembersUnreadNotifications()
    {
        await SeedAsync(Member, minutesAgo: 1);
        await SeedAsync(Member, minutesAgo: 2);
        await SeedAsync(Member, minutesAgo: 3, readAt: _clock.UtcNow);
        await SeedAsync(OtherMember, minutesAgo: 1);

        var count = await _service.CountUnreadAsync(Member);

        count.ShouldBe(2);
    }

    [Fact]
    public async Task CountUnread_ForAMemberWithNothing_IsZero()
    {
        var count = await _service.CountUnreadAsync(Member);

        count.ShouldBe(0);
    }

    [Fact]
    public async Task CountUnread_WhenStorageFails_Throws()
    {
        _store.FailureToThrow = new NotificationStoreException("boom", new InvalidOperationException());

        await Should.ThrowAsync<NotificationStoreException>(async () => await _service.CountUnreadAsync(Member));
    }

    [Fact]
    public async Task MarkRead_SetsTheTimeOfTheCall()
    {
        var stored = await SeedAsync(Member, minutesAgo: 5);

        var found = await _service.MarkReadAsync(Member, stored.Id);

        found.ShouldBeTrue();
        ReadAtOf(stored.Id).ShouldBe(_clock.UtcNow);
    }

    [Fact]
    public async Task MarkRead_Twice_KeepsTheTimeOfTheFirstCall()
    {
        var stored = await SeedAsync(Member, minutesAgo: 5);
        var firstCall = _clock.UtcNow;
        await _service.MarkReadAsync(Member, stored.Id);

        _clock.Advance(TimeSpan.FromMinutes(10));
        var found = await _service.MarkReadAsync(Member, stored.Id);

        found.ShouldBeTrue();
        ReadAtOf(stored.Id).ShouldBe(firstCall);
    }

    [Fact]
    public async Task MarkRead_OfAnotherMembersNotification_ReturnsFalse_AndChangesNothing()
    {
        var foreign = await SeedAsync(OtherMember, minutesAgo: 5);

        var found = await _service.MarkReadAsync(Member, foreign.Id);

        found.ShouldBeFalse();
        ReadAtOf(foreign.Id).ShouldBeNull();
    }

    [Fact]
    public async Task MarkRead_OfAnUnknownId_ReturnsFalse()
    {
        var found = await _service.MarkReadAsync(Member, NextId());

        found.ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-id")]
    [InlineData("../notifications/other")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public async Task MarkRead_WithAMalformedId_ReturnsFalse_WithoutReadingStorage(string id)
    {
        var found = await _service.MarkReadAsync(Member, id);

        found.ShouldBeFalse();
        _store.MarkReadCalls.ShouldBe(0);
    }

    [Fact]
    public async Task MarkRead_WhenStorageFails_Throws()
    {
        var stored = await SeedAsync(Member, minutesAgo: 5);
        _store.FailureToThrow = new NotificationStoreException("boom", new InvalidOperationException());

        await Should.ThrowAsync<NotificationStoreException>(async () => await _service.MarkReadAsync(Member, stored.Id));
    }

    [Fact]
    public async Task MarkAllRead_MarksTheMembersUnread_AndLeavesTheRestAlone()
    {
        var alreadyRead = _clock.UtcNow.AddHours(-1);
        var unreadA = await SeedAsync(Member, minutesAgo: 1);
        var unreadB = await SeedAsync(Member, minutesAgo: 2);
        var read = await SeedAsync(Member, minutesAgo: 3, readAt: alreadyRead);
        var foreign = await SeedAsync(OtherMember, minutesAgo: 1);

        var marked = await _service.MarkAllReadAsync(Member);

        marked.ShouldBe(2);
        ReadAtOf(unreadA.Id).ShouldBe(_clock.UtcNow);
        ReadAtOf(unreadB.Id).ShouldBe(_clock.UtcNow);
        ReadAtOf(read.Id).ShouldBe(alreadyRead);
        ReadAtOf(foreign.Id).ShouldBeNull();
    }

    [Fact]
    public async Task MarkAllRead_WhenNothingIsUnread_ReturnsZero()
    {
        await SeedAsync(Member, minutesAgo: 1, readAt: _clock.UtcNow);

        var marked = await _service.MarkAllReadAsync(Member);

        marked.ShouldBe(0);
    }

    [Fact]
    public async Task MarkAllRead_WorksThroughSeveralBatches()
    {
        _store.MarkBatchSize = 2;

        for (var minutesAgo = 1; minutesAgo <= 5; minutesAgo++)
        {
            await SeedAsync(Member, minutesAgo);
        }

        var marked = await _service.MarkAllReadAsync(Member);

        marked.ShouldBe(5);
        (await _service.CountUnreadAsync(Member)).ShouldBe(0);
        _store.MarkBatchCalls.ShouldBe(4);
    }

    [Fact]
    public async Task MarkAllRead_StopsAtTheSafetyLimit_AndLeavesTheRestUnread()
    {
        _store.MarkBatchSize = 1;

        for (var minutesAgo = 1; minutesAgo <= MarkAllBatchLimit + 5; minutesAgo++)
        {
            await SeedAsync(Member, minutesAgo);
        }

        var marked = await _service.MarkAllReadAsync(Member);

        marked.ShouldBe(MarkAllBatchLimit);
        (await _service.CountUnreadAsync(Member)).ShouldBe(5);
        _store.MarkBatchCalls.ShouldBe(MarkAllBatchLimit);
    }

    [Fact]
    public async Task MarkAllRead_WhenCancelled_Throws_AndMarksNothing()
    {
        await SeedAsync(Member, minutesAgo: 1);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await _service.MarkAllReadAsync(Member, cancellation.Token));

        (await _service.CountUnreadAsync(Member)).ShouldBe(1);
    }

    [Fact]
    public async Task MarkAllRead_WhenStorageFails_Throws()
    {
        await SeedAsync(Member, minutesAgo: 1);
        _store.FailureToThrow = new NotificationStoreException("boom", new InvalidOperationException());

        await Should.ThrowAsync<NotificationStoreException>(async () => await _service.MarkAllReadAsync(Member));
    }

    #endregion

    #region Private Methods

    private string NextId()
    {
        return (++_lastId).ToString("x32");
    }

    // Stores a notification that was created the given number of minutes ago; later calls get a higher id
    private async Task<InboxNotification> SeedAsync(
        string recipientId,
        int minutesAgo,
        DateTimeOffset? readAt = null,
        DateTimeOffset? expiresAt = null)
    {
        var notification = new InboxNotification
        {
            Id = NextId(),
            RecipientId = recipientId,
            Key = Key,
            Data = new Dictionary<string, string> { [OrderNumberName] = OrderNumber },
            CreatedAt = _clock.UtcNow.AddMinutes(-minutesAgo),
            ExpiresAt = expiresAt ?? _clock.UtcNow.AddDays(RetentionDays),
            ReadAt = readAt
        };

        (await _store.TryCreateAsync(notification)).ShouldBeTrue();

        return notification;
    }

    // Follows the cursor from the first page to the last, returning every id seen and how many pages it took
    private async Task<(List<string> Ids, int Pages)> ReadAllPagesAsync(int limit)
    {
        var ids = new List<string>();
        string? cursor = null;
        var pages = 0;

        do
        {
            var page = await _service.ListAsync(Member, limit, cursor);

            page.ShouldNotBeNull();
            ids.AddRange(page.Items.Select(item => item.Id));
            cursor = page.NextCursor;
            pages++;
        }
        while (cursor is not null);

        return (ids, pages);
    }

    private DateTimeOffset? ReadAtOf(string id)
    {
        return _store.Documents.Single(document => document.Id == id).ReadAt;
    }

    #endregion
}
