// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/Profiles/HandleFormat.cs
// Version : 1.0.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : What makes a handle usable: how it is normalized, which shapes are valid and
// which are reserved or blocked. Pure rules, shared by everything that deals with handles.

using TeamSamsara.Modules.Identity.Models;

namespace TeamSamsara.Modules.Identity.Handlers;

public static class HandleFormat
{
    #region Fields

    private const char HandleSeparator = '_';

    #endregion

    #region Public Methods

    // The handle as the member typed it, lowercased and without a leading @
    public static string Normalize(string? handle)
    {
        return handle?.Trim().TrimStart('@').ToLowerInvariant() ?? string.Empty;
    }

    // A handle fits the length limits, uses only a-z, 0-9 and _, and does not start or end with _
    public static bool IsValid(string handle, ProfileSettings settings)
    {
        return handle.Length >= settings.HandleMinLength
            && handle.Length <= settings.HandleMaxLength
            && handle.All(IsHandleCharacter)
            && handle[0] != HandleSeparator
            && handle[^1] != HandleSeparator;
    }

    // Reserved and blocked handles are refused the same way a taken one is
    public static bool IsForbidden(string handle)
    {
        return ReservedHandles.IsReserved(handle) || BlockedWords.IsBlockedHandle(handle);
    }

    // Normalizes the handle and checks it can be used. When it cannot, says why: a badly
    // formed handle is invalid, and a reserved or blocked one is refused as taken.
    public static bool TryNormalize(
        string? raw,
        ProfileSettings settings,
        out string handle,
        out ProfileStatus refusal)
    {
        handle = Normalize(raw);
        refusal = ProfileStatus.Success;

        if (!IsValid(handle, settings))
        {
            refusal = ProfileStatus.InvalidHandle;
        }
        else if (IsForbidden(handle))
        {
            refusal = ProfileStatus.HandleTaken;
        }

        return refusal == ProfileStatus.Success;
    }

    #endregion

    #region Private Methods

    private static bool IsHandleCharacter(char character)
    {
        return character is >= 'a' and <= 'z' or >= '0' and <= '9' or HandleSeparator;
    }

    #endregion
}
