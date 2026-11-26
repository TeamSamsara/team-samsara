// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification.Tests/InboxRetentionTests.cs
// Version : 1.0.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Proves the clearing rules: only an unanswered sign-in notice is protected, and when an inbox is over its limit the oldest read notifications go first, then the oldest unread ones, never a protected one.

using Shouldly;
using TeamSamsara.Modules.Notification.Models;
using TeamSamsara.Shared.Notification;

namespace TeamSamsara.Modules.Notification.Tests;

public class InboxRetentionTests
{
    #region Fields

    private const string Member = "uid-member-1";
    private const string OtherKey = "order.shipped";
    private const int Limit = 3;

    private static readonly DateTimeOffset _now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    #endregion

    #region Public Methods

    [Theory]
    [InlineData(NotificationKeys.NewLogin, false, true)]
    [InlineData(NotificationKeys.NewLogin, true, false)]
    [InlineData(OtherKey, false, false)]
    [InlineData(OtherKey, true, false)]
    public void IsUnansweredSignIn_OnlyForAnUnreadSignInNotice(string key, bool read, bool expected)
    {
        InboxRetention.IsUnansweredSignIn(Notification("a", 1, key, read)).ShouldBe(expected);
    }

    [Fact]
    public void IsClearable_IsTheOppositeOfUnansweredSignIn()
    {
        InboxRetention.IsClearable(Notification("a", 1, NotificationKeys.NewLogin, read: false)).ShouldBeFalse();
        InboxRetention.IsClearable(Notification("b", 1, NotificationKeys.NewLogin, read: true)).ShouldBeTrue();
        InboxRetention.IsClearable(Notification("c", 1, OtherKey, read: false)).ShouldBeTrue();
    }

    [Fact]
    public void SelectForTrim_WithinTheLimit_SelectsNothing()
    {
        var inbox = new[] { Notification("a", 1), Notification("b", 2), Notification("c", 3) };

        InboxRetention.SelectForTrim(inbox, Limit).ShouldBeEmpty();
    }

    [Fact]
    public void SelectForTrim_TakesTheOldestReadFirst_EvenWhenAnUnreadOneIsOlder()
    {
        // Newest first: d (unread), c (read), b (unread), a (unread, oldest)
        var inbox = new[]
        {
            Notification("d", 1),
            Notification("c", 2, read: true),
            Notification("b", 3),
            Notification("a", 4)
        };

        InboxRetention.SelectForTrim(inbox, Limit).Select(item => item.Id).ShouldBe(new[] { "c" });
    }

    [Fact]
    public void SelectForTrim_WhenNothingIsRead_TakesTheOldestUnread()
    {
        var inbox = new[] { Notification("d", 1), Notification("c", 2), Notification("b", 3), Notification("a", 4) };

        InboxRetention.SelectForTrim(inbox, Limit).Select(item => item.Id).ShouldBe(new[] { "a" });
    }

    [Fact]
    public void SelectForTrim_WithSeveralExcess_TakesReadOnesOldestFirst_ThenUnreadOldestFirst()
    {
        // Newest first; limit 2 leaves an excess of 3
        var inbox = new[]
        {
            Notification("e", 1),
            Notification("d", 2, read: true),
            Notification("c", 3),
            Notification("b", 4, read: true),
            Notification("a", 5)
        };

        InboxRetention.SelectForTrim(inbox, 2).Select(item => item.Id).ShouldBe(new[] { "b", "d", "a" });
    }

    [Fact]
    public void SelectForTrim_NeverSelectsAnUnansweredSignIn()
    {
        var inbox = new[]
        {
            Notification("d", 1),
            Notification("c", 2, NotificationKeys.NewLogin),
            Notification("b", 3, NotificationKeys.NewLogin),
            Notification("a", 4, NotificationKeys.NewLogin)
        };

        InboxRetention.SelectForTrim(inbox, Limit).ShouldBe(new[] { inbox[0] });
    }

    [Fact]
    public void SelectForTrim_WhenOnlyProtectedOnesAreOldEnough_StaysOverTheLimit()
    {
        var inbox = new[]
        {
            Notification("c", 1, NotificationKeys.NewLogin),
            Notification("b", 2, NotificationKeys.NewLogin),
            Notification("a", 3, NotificationKeys.NewLogin)
        };

        InboxRetention.SelectForTrim(inbox, 1).ShouldBeEmpty();
    }

    [Fact]
    public void SelectForTrim_AnAnsweredSignInCanBeTrimmed()
    {
        var inbox = new[]
        {
            Notification("d", 1),
            Notification("c", 2),
            Notification("b", 3),
            Notification("a", 4, NotificationKeys.NewLogin, read: true)
        };

        InboxRetention.SelectForTrim(inbox, Limit).Select(item => item.Id).ShouldBe(new[] { "a" });
    }

    #endregion

    #region Private Methods

    private static InboxNotification Notification(
        string id,
        int minutesAgo,
        string key = OtherKey,
        bool read = false)
    {
        return new InboxNotification
        {
            Id = id,
            RecipientId = Member,
            Key = key,
            Data = new Dictionary<string, string>(),
            CreatedAt = _now.AddMinutes(-minutesAgo),
            ExpiresAt = _now.AddDays(90),
            ReadAt = read ? _now : null
        };
    }

    #endregion
}
