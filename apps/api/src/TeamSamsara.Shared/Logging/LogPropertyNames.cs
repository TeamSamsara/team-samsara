// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Logging/LogPropertyNames.cs
// Version : 1.0.0
// Latest commit: feature/string-magic-value-conventions
// Author : Gerrah
// Purpose : Structured-log property names, so log queries filtering on these fields stay
// correct even if the source ever changes wording.

namespace TeamSamsara.Shared.Logging;

public static class LogPropertyNames
{
    #region Fields

    public const string CorrelationId = "CorrelationId";
    public const string Environment = "Environment";
    public const string Service = "Service";

    #endregion
}
