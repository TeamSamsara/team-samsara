// File : /team-samsara/apps/api/src/TeamSamsara.Modules.PingPong/PingPongConstants.cs
// Version : 1.0.0
// Latest commit: feature/string-magic-value-conventions
// Author : Gerrah
// Purpose : Route paths and response text shared between PingPongModule and its tests -
// module-owned, not Shared, since these values are specific to this module alone.

namespace TeamSamsara.Modules.PingPong;

public static class PingPongConstants
{
    #region Fields

    public const string PingRoute = "/ping";
    public const string PingSecureRoute = "/ping/secure";
    public const string PongResponse = "pong";

    #endregion
}
