// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification.Tests/NotificationMemberDataEraserTests.cs
// Version : 1.0.0
// Latest commit: feat/notification-erasure
// Author : Gerrah
// Purpose : Proves the eraser removes all of one member's notifications across batches, leaves other members alone, can be repeated, and lets failures and cancellation through to the purge.

using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TeamSamsara.Modules.Notification.Handlers;
using TeamSamsara.Modules.Notification.Models;
using TeamSamsara.Modules.Notification.Repositories;
using TeamSamsara.Modules.Notification.Tests.Fakes;

namespace TeamSamsara.Modules.Notification.Tests;

public class NotificationMemberDataEraserTests
{
    #region Fields

    private const string Member = "uid-member-1";
    private const string OtherMember = "uid-member-2";
    private const string Key = "order.shipped";
    private const int ExpiryDays = 90;
    private const int SeveralNotifications = 5;
    private const int SmallBatchSize = 2;
    private const int BatchesForFiveInTwos = 3;

    private static readonly DateTimeOffset _now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryInboxNotificationStore _store = new();
    private readonly NotificationMemberDataEraser _eraser;

    #endregion

    #region Constructors

    public NotificationMemberDataEraserTests()
    {
        _eraser = new NotificationMemberDataEraser(_store, NullLogger<NotificationMemberDataEraser>.Instance);
    }

    #endregion

    #region Public Methods

    [Fact]
    public async Task Erase_RemovesEveryNotificationOfTheMember()
    {
        await SeedAsync(Member, SeveralNotifications);

        await _eraser.EraseAsync(Member);

        _store.Documents.ShouldBeEmpty();
    }

    [Fact]
    public async Task Erase_LeavesOtherMembersNotificationsAlone()
    {
        await SeedAsync(Member, SeveralNotifications);
        await SeedAsync(OtherMember, SmallBatchSize);

        await _eraser.EraseAsync(Member);

        _store.Documents.Count.ShouldBe(SmallBatchSize);
        _store.Documents.ShouldAllBe(document => document.RecipientId == OtherMember);
    }

    [Fact]
    public async Task Erase_WithMoreThanOneBatch_KeepsGoingUntilNoneAreLeft()
    {
        _store.DeleteBatchSize = SmallBatchSize;
        await SeedAsync(Member, SeveralNotifications);

        await _eraser.EraseAsync(Member);

        _store.Documents.ShouldBeEmpty();
        _store.DeleteBatchCalls.ShouldBe(BatchesForFiveInTwos + 1);
    }

    [Fact]
    public async Task Erase_ForAMemberWithNoNotifications_Succeeds()
    {
        await _eraser.EraseAsync(Member);

        _store.DeleteBatchCalls.ShouldBe(1);
    }

    [Fact]
    public async Task Erase_Twice_IsHarmless()
    {
        await SeedAsync(Member, SeveralNotifications);

        await _eraser.EraseAsync(Member);
        await _eraser.EraseAsync(Member);

        _store.Documents.ShouldBeEmpty();
    }

    [Fact]
    public async Task Erase_WhenTheStoreFails_LetsTheFailureThrough()
    {
        await SeedAsync(Member, SeveralNotifications);
        _store.FailureToThrow = new NotificationStoreException("Firestore is down.", new InvalidOperationException());

        await Should.ThrowAsync<NotificationStoreException>(() => _eraser.EraseAsync(Member));

        _store.Documents.Count.ShouldBe(SeveralNotifications);
    }

    [Fact]
    public async Task Erase_WhenCancelled_ThrowsWithoutTouchingTheStore()
    {
        await SeedAsync(Member, SeveralNotifications);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => _eraser.EraseAsync(Member, cancellation.Token));

        _store.DeleteBatchCalls.ShouldBe(0);
        _store.Documents.Count.ShouldBe(SeveralNotifications);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Erase_WithABlankMemberId_Throws(string userId)
    {
        await Should.ThrowAsync<ArgumentException>(() => _eraser.EraseAsync(userId));

        _store.DeleteBatchCalls.ShouldBe(0);
    }

    #endregion

    #region Private Methods

    // Stores the given number of notifications for the member
    private async Task SeedAsync(string recipientId, int count)
    {
        for (var index = 0; index < count; index++)
        {
            var created = await _store.TryCreateAsync(new InboxNotification
            {
                Id = $"{recipientId}-{index}",
                RecipientId = recipientId,
                Key = Key,
                Data = new Dictionary<string, string>(),
                CreatedAt = _now,
                ExpiresAt = _now.AddDays(ExpiryDays)
            });

            created.ShouldBeTrue();
        }
    }

    #endregion
}
