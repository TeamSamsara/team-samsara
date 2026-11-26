// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification.Tests/NotificationIdsTests.cs
// Version : 1.0.0
// Latest commit: task/notifications-api
// Author : Gerrah
// Purpose : Proves only 32 or 64 lowercase hex characters count as a notification id, so nothing else reaches storage.

using Shouldly;
using TeamSamsara.Modules.Notification.Models;

namespace TeamSamsara.Modules.Notification.Tests;

public class NotificationIdsTests
{
    #region Public Methods

    [Theory]
    [InlineData(32)]
    [InlineData(64)]
    public void IsValid_ForLowercaseHexOfTheRightLength_IsTrue(int length)
    {
        NotificationIds.IsValid(new string('a', length)).ShouldBeTrue();
        NotificationIds.IsValid(new string('7', length)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(31)]
    [InlineData(33)]
    [InlineData(63)]
    [InlineData(65)]
    public void IsValid_ForTheWrongLength_IsFalse(int length)
    {
        NotificationIds.IsValid(new string('a', length)).ShouldBeFalse();
    }

    [Fact]
    public void IsValid_ForNull_IsFalse()
    {
        NotificationIds.IsValid(null).ShouldBeFalse();
    }

    [Fact]
    public void IsValid_ForUppercaseHex_IsFalse()
    {
        NotificationIds.IsValid(new string('A', 32)).ShouldBeFalse();
    }

    [Fact]
    public void IsValid_ForCharactersThatAreNotHex_IsFalse()
    {
        NotificationIds.IsValid(new string('g', 32)).ShouldBeFalse();
        NotificationIds.IsValid(new string('a', 31) + "/").ShouldBeFalse();
        NotificationIds.IsValid(new string('a', 31) + " ").ShouldBeFalse();
    }

    [Fact]
    public void IsValid_ForAValidIdFollowedByANewline_IsFalse()
    {
        NotificationIds.IsValid(new string('a', 32) + "\n").ShouldBeFalse();
    }

    #endregion
}
