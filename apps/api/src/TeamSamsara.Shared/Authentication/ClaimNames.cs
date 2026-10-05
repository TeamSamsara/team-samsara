// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Authentication/ClaimNames.cs
// Version : 1.1.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : Claim key names carried on Firebase ID tokens: our custom accessLevel claim, and
// Firebase's standard auth_time claim.

namespace TeamSamsara.Shared.Authentication;

public static class ClaimNames
{
    #region Fields

    public const string AccessLevel = "accessLevel";

    // Firebase's standard claim: when this sign-in happened, in Unix seconds. It stays the same
    // across token refreshes, so it identifies one sign-in event.
    public const string AuthTime = "auth_time";

    #endregion
}
