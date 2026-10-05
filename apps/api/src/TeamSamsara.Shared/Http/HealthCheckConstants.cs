// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Http/HealthCheckConstants.cs
// Version : 1.0.0
// Latest commit: feature/string-magic-value-conventions
// Author : Gerrah
// Purpose : Health check tag/name/message values.

namespace TeamSamsara.Shared.Http;

public static class HealthCheckConstants
{
    #region Fields

    public const string ReadyTag = "ready";
    public const string FirestoreCheckName = "firestore";
    public const string FirestoreUnreachableMessage = "Firestore is not reachable.";

    #endregion
}
