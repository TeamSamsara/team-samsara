// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification.Tests/InboxCursorTests.cs
// Version : 1.0.0
// Latest commit: task/notifications-api
// Author : Gerrah
// Purpose : Proves a cursor survives the trip to the client and back unchanged, and that anything that is not exactly what Encode produces is refused.

using System.Buffers.Text;
using System.Text;
using Shouldly;
using TeamSamsara.Modules.Notification.Models;

namespace TeamSamsara.Modules.Notification.Tests;

public class InboxCursorTests
{
    #region Fields

    private static readonly string _validId = new('a', 32);
    private static readonly long _validTicks = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero).UtcTicks;

    #endregion

    #region Public Methods

    public static TheoryData<string?> MalformedCursors()
    {
        return new TheoryData<string?>
        {
            null,
            "",
            "garbage",
            "v2." + Payload($"{_validTicks}|{_validId}"),
            "v1.",
            "v1.!!!",
            "v1." + Base64Url.EncodeToString(new byte[] { 0xFF, 0xFE }),
            Cursor($"{_validTicks}"),
            Cursor($"{_validTicks}|"),
            Cursor($"|{_validId}"),
            Cursor($"{_validTicks}|{_validId}|extra"),
            Cursor($"abc|{_validId}"),
            Cursor($"-1|{_validId}"),
            Cursor($"+{_validTicks}|{_validId}"),
            Cursor($"99999999999999999999|{_validId}"),
            Cursor($"{DateTime.MaxValue.Ticks + 1}|{_validId}"),
            Cursor($"{_validTicks}|{new string('A', 32)}"),
            Cursor($"{_validTicks}|{new string('a', 31)}"),
            Cursor($"{_validTicks}|{new string('a', 300)}"),
            "v1." + new string('A', 300)
        };
    }

    [Fact]
    public void ACursor_SurvivesEncodingAndParsing()
    {
        var original = new InboxCursor(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero).AddTicks(1_234_567), _validId);

        var parsed = InboxCursor.TryParse(original.Encode(), out var cursor);

        parsed.ShouldBeTrue();
        cursor.ShouldBe(original);
        cursor!.CreatedAt.UtcTicks.ShouldBe(original.CreatedAt.UtcTicks);
    }

    [Fact]
    public void ACursor_BuiltFromALongIdSurvivesToo()
    {
        var original = new InboxCursor(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero), new string('b', 64));

        InboxCursor.TryParse(original.Encode(), out var cursor).ShouldBeTrue();

        cursor.ShouldBe(original);
    }

    [Fact]
    public void ACursorWithAnOffset_IsReadBackAsTheSameInstant()
    {
        var original = new InboxCursor(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(-5)), _validId);

        InboxCursor.TryParse(original.Encode(), out var cursor).ShouldBeTrue();

        cursor.CreatedAt.ShouldBe(original.CreatedAt);
        cursor.CreatedAt.Offset.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void AnEncodedCursor_CarriesTheVersionAndOnlyUrlSafeCharacters()
    {
        var text = new InboxCursor(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), _validId).Encode();

        text.ShouldStartWith("v1.");
        text.ShouldMatch(@"^v1\.[A-Za-z0-9_-]+\z");
        text.Length.ShouldBeLessThan(200);
    }

    [Theory]
    [MemberData(nameof(MalformedCursors))]
    public void AMalformedCursor_IsRefused(string? text)
    {
        var parsed = InboxCursor.TryParse(text, out var cursor);

        parsed.ShouldBeFalse();
        cursor.ShouldBeNull();
    }

    #endregion

    #region Private Methods

    private static string Payload(string text)
    {
        return Base64Url.EncodeToString(Encoding.UTF8.GetBytes(text));
    }

    // A cursor with the right version prefix around the given payload text
    private static string Cursor(string text)
    {
        return "v1." + Payload(text);
    }

    #endregion
}
