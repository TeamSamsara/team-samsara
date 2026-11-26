// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/Models/InboxCursor.cs
// Version : 1.0.0
// Latest commit: task/notifications-api
// Author : Gerrah
// Purpose : Marks where a page of the inbox ended (the last notification's time and id), as an opaque versioned text the client hands back to get the next page.

using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace TeamSamsara.Modules.Notification.Models;

public sealed record InboxCursor(DateTimeOffset CreatedAt, string Id)
{
    #region Fields

    // Bumped if the format ever changes, so an old cursor is refused instead of misread
    private const string Prefix = "v1.";
    private const char Separator = '|';

    // A real cursor is about 120 characters; anything longer is not worth decoding
    private const int MaxLength = 200;

    private static readonly UTF8Encoding _strictUtf8 = new(false, true);

    #endregion

    #region Public Methods

    // The text form sent to the client: the version prefix, then the UTC ticks and the id, base64url encoded
    public string Encode()
    {
        var text = $"{CreatedAt.UtcTicks.ToString(CultureInfo.InvariantCulture)}{Separator}{Id}";

        return Prefix + Base64Url.EncodeToString(Encoding.UTF8.GetBytes(text));
    }

    // Reads a cursor back; false for anything that is not exactly what Encode produces
    public static bool TryParse(string? value, [NotNullWhen(true)] out InboxCursor? cursor)
    {
        cursor = null;

        if (!TryDecode(value, out var text))
        {
            return false;
        }

        var parts = text.Split(Separator);

        if (parts.Length != 2 || !TryReadTicks(parts[0], out var ticks) || !NotificationIds.IsValid(parts[1]))
        {
            return false;
        }

        cursor = new InboxCursor(new DateTimeOffset(ticks, TimeSpan.Zero), parts[1]);

        return true;
    }

    #endregion

    #region Private Methods

    // Removes the version prefix and the base64url layer; false when either is not what Encode produces
    private static bool TryDecode(string? value, [NotNullWhen(true)] out string? text)
    {
        text = null;

        if (value is null || value.Length > MaxLength || !value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            var bytes = Base64Url.DecodeFromChars(value.AsSpan(Prefix.Length));
            text = _strictUtf8.GetString(bytes);

            return true;
        }
        catch (Exception exception) when (exception is FormatException or DecoderFallbackException)
        {
            return false;
        }
    }

    // Reads the ticks as digits only, no sign, and refuses a value past the last date a DateTimeOffset can hold
    private static bool TryReadTicks(string text, out long ticks)
    {
        return long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out ticks)
            && ticks <= DateTime.MaxValue.Ticks;
    }

    #endregion
}
