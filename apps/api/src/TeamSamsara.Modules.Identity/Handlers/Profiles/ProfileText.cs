// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/Profiles/ProfileText.cs
// Version : 1.0.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : The rules for the text a member writes on their profile: display name and bio.
// Pure rules, so they are easy to test on their own.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Handlers;

public static class ProfileText
{
    #region Fields

    private const char LineFeed = '\n';
    private const char CarriageReturn = '\r';

    #endregion

    #region Public Methods

    // A name needs at least one visible character, must fit the limits, has no control
    // characters and no blocked word
    public static bool IsValidDisplayName([NotNullWhen(true)] string? displayName, ProfileSettings settings)
    {
        if (string.IsNullOrEmpty(displayName))
        {
            return false;
        }

        var length = LengthOf(displayName);

        return length >= settings.DisplayNameMinLength
            && length <= settings.DisplayNameMaxLength
            && !displayName.Any(char.IsControl)
            && !BlockedWords.IsBlockedName(displayName);
    }

    // No bio is valid; a bio must fit the limit and may only use line breaks as control characters
    public static bool IsValidBio(string? bio, ProfileSettings settings)
    {
        if (bio is null)
        {
            return true;
        }

        return LengthOf(bio) <= settings.BioMaxLength
            && !bio.Any(IsForbiddenControl);
    }

    // Counts what a reader sees as characters, so an emoji or an accented letter counts once
    public static int LengthOf(string text) => new StringInfo(text).LengthInTextElements;

    #endregion

    #region Private Methods

    private static bool IsForbiddenControl(char character)
    {
        return char.IsControl(character) && character != LineFeed && character != CarriageReturn;
    }

    #endregion
}
