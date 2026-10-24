// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/Profiles/HandleCandidates.cs
// Version : 1.0.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : The handles to try for a member who has none, best first: their display name,
// then numbered variants, then a neutral last resort. Pure, so it is easy to test.

using System.Globalization;
using System.Text;
using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Handlers;

public static class HandleCandidates
{
    #region Fields

    private const string NeutralHandle = "member";
    private const int SuffixDigits = 4;
    private const int SuffixAttempts = 8;
    private const int SuffixMin = 1000;
    private const int SuffixMax = 10000;
    private const int LastResortIdLength = 8;

    #endregion

    #region Public Methods

    // The display name itself, then numbered variants of it. A name that cannot be used as a
    // handle, such as the neutral "Member", is replaced by the neutral handle.
    public static IEnumerable<string> For(string displayName, ProfileSettings settings)
    {
        var seed = SeedFrom(displayName, settings.HandleMaxLength);
        var usable = seed.Length >= settings.HandleMinLength && !HandleFormat.IsForbidden(seed);
        var stem = usable ? seed : NeutralHandle;

        if (usable)
        {
            yield return seed;
        }

        var stemLength = Math.Min(stem.Length, settings.HandleMaxLength - SuffixDigits);

        for (var attempt = 0; attempt < SuffixAttempts; attempt++)
        {
            var number = Random.Shared.Next(SuffixMin, SuffixMax).ToString(CultureInfo.InvariantCulture);

            yield return stem[..stemLength] + number;
        }

        yield return LastResort(settings.HandleMaxLength);
    }

    #endregion

    #region Private Methods

    // The display name reduced to lowercase letters and digits, cut to the handle length
    private static string SeedFrom(string displayName, int maxLength)
    {
        var builder = new StringBuilder();

        foreach (var character in displayName.ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            if (character is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                builder.Append(character);
            }
        }

        var seed = builder.ToString();

        return seed.Length > maxLength ? seed[..maxLength] : seed;
    }

    // The neutral handle followed by a short random id, cut to fit the handle length
    private static string LastResort(int maxLength)
    {
        var id = Guid.NewGuid().ToString("N")[..LastResortIdLength];

        return NeutralHandle + id[..Math.Min(LastResortIdLength, maxLength - NeutralHandle.Length)];
    }

    #endregion
}
