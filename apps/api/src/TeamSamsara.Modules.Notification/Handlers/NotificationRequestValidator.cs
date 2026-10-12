// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Notification/Handlers/NotificationRequestValidator.cs
// Version : 1.0.0
// Latest commit: feat/notification-send-path
// Author : Gerrah
// Purpose : Enforces the format and size rules of the notification contract, throwing ArgumentException for a malformed request.

using System.Text.RegularExpressions;
using TeamSamsara.Shared.Notification;

namespace TeamSamsara.Modules.Notification.Handlers;

public static class NotificationRequestValidator
{
    #region Fields

    private const int MaxDataEntries = 20;
    private const int MaxDataValueLength = 500;

    private static readonly Regex _recipientPattern = new(@"^[A-Za-z0-9_-]{1,128}\z", RegexOptions.CultureInvariant);
    private static readonly Regex _keyPattern = new(@"^[a-z][a-z0-9-]{0,31}(\.[a-z][a-z0-9-]{0,31}){1,3}\z", RegexOptions.CultureInvariant);
    private static readonly Regex _dedupeKeyPattern = new(@"^[A-Za-z0-9._:-]{1,100}\z", RegexOptions.CultureInvariant);
    private static readonly Regex _dataNamePattern = new(@"^[a-z][A-Za-z0-9]{0,39}\z", RegexOptions.CultureInvariant);

    #endregion

    #region Public Methods

    // Throws ArgumentException when the request breaks a rule of the notification contract
    public static void Validate(NotificationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Require(request.RecipientId, _recipientPattern, nameof(request.RecipientId));
        Require(request.Key, _keyPattern, nameof(request.Key));

        if (request.DedupeKey is not null)
        {
            Require(request.DedupeKey, _dedupeKeyPattern, nameof(request.DedupeKey));
        }

        ValidateData(request.Data);
    }

    #endregion

    #region Private Methods

    // Throws unless the value is present and matches the pattern
    private static void Require(string? value, Regex pattern, string name)
    {
        if (value is null || !pattern.IsMatch(value))
        {
            throw new ArgumentException($"{name} is not valid.", name);
        }
    }

    // Throws when the data has too many entries, a badly named entry or a missing or oversized value
    private static void ValidateData(IReadOnlyDictionary<string, string>? data)
    {
        if (data is null)
        {
            return;
        }

        if (data.Count > MaxDataEntries)
        {
            throw new ArgumentException($"Data has more than {MaxDataEntries} entries.", nameof(data));
        }

        foreach (var (name, value) in data)
        {
            Require(name, _dataNamePattern, nameof(data));

            if (value is null || value.Length > MaxDataValueLength)
            {
                throw new ArgumentException($"A data value is missing or longer than {MaxDataValueLength} characters.", nameof(data));
            }
        }
    }

    #endregion
}
