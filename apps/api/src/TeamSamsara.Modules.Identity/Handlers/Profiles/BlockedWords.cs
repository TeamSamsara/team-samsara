// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/Profiles/BlockedWords.cs
// Version : 1.0.0
// Latest commit: task/api-profile-onboarding
// Author : Gerrah
// Purpose : Tells whether a display name or handle contains a blocked word,
// using the word lists embedded in this assembly.

using System.Globalization;
using System.Reflection;
using System.Text;

namespace TeamSamsara.Modules.Identity.Handlers;

public static class BlockedWords
{
    #region Fields

    private const string ResourcePrefix = "BlockedWords.";
    private const string ResourceSuffix = ".txt";

    // Shorter words are only blocked when they are the whole handle, so "class" stays allowed
    private const int HandleMinimumMatchLength = 4;

    private static readonly Dictionary<char, char> LookAlikes = new()
    {
        ['0'] = 'o',
        ['1'] = 'i',
        ['3'] = 'e',
        ['4'] = 'a',
        ['5'] = 's',
        ['7'] = 't',
        ['8'] = 'b',
        ['$'] = 's',
        ['@'] = 'a',
        ['!'] = 'i',
        ['+'] = 't'
    };

    private static readonly Lazy<IReadOnlyList<Entry>> Entries = new(LoadEntries);

    #endregion

    #region Public Methods

    // True if a whole word or phrase of the name is blocked
    public static bool IsBlockedName(string name)
    {
        var padded = $" {Fold(name)} ";

        return Entries.Value.Any(entry => padded.Contains(entry.Padded, StringComparison.Ordinal));
    }

    // True if the handle contains a blocked word. Stricter than for names, since a handle
    // has no spaces to separate its words.
    public static bool IsBlockedHandle(string handle)
    {
        var squashed = Fold(handle).Replace(" ", string.Empty);

        return Entries.Value.Any(entry => entry.Squashed.Length >= HandleMinimumMatchLength
            ? squashed.Contains(entry.Squashed, StringComparison.Ordinal)
            : squashed == entry.Squashed);
    }

    #endregion

    #region Private Methods

    // Reads every embedded list and keeps each word in its folded form
    private static IReadOnlyList<Entry> LoadEntries()
    {
        var assembly = typeof(BlockedWords).Assembly;
        var entries = new List<Entry>();

        var resources = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal)
                && name.EndsWith(ResourceSuffix, StringComparison.Ordinal));

        foreach (var resource in resources)
        {
            using var stream = assembly.GetManifestResourceStream(resource)!;
            using var reader = new StreamReader(stream);

            while (reader.ReadLine() is { } line)
            {
                var folded = Fold(line);

                if (folded.Length > 0)
                {
                    entries.Add(new Entry($" {folded} ", folded.Replace(" ", string.Empty)));
                }
            }
        }

        return entries;
    }

    // Reduces text to lowercase letters in single-spaced words: accents removed, look-alike
    // digits and symbols turned into letters, anything else a space, repeated letters merged
    private static string Fold(string text)
    {
        var builder = new StringBuilder(text.Length);
        var decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);

        foreach (var original in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(original) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var character = LookAlikes.GetValueOrDefault(original, original);

            if (char.IsLetter(character))
            {
                if (builder.Length == 0 || builder[^1] != character)
                {
                    builder.Append(character);
                }
            }
            else if (builder.Length > 0 && builder[^1] != ' ')
            {
                builder.Append(' ');
            }
        }

        return builder.ToString().Trim();
    }

    #endregion

    #region Nested Types

    // A blocked word: padded with spaces to match whole words, and with spaces removed to
    // match inside a handle
    private sealed record Entry(string Padded, string Squashed);

    #endregion
}
