// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Http/HttpConstants.cs
// Version : 1.0.0
// Latest commit: feature/string-magic-value-conventions
// Author : Gerrah
// Purpose : HTTP protocol values our code depends on matching exactly, not covered by any
// typed API in the frameworks we use.

namespace TeamSamsara.Shared.Http;

public static class HttpConstants
{
    #region Fields

    public const string BearerPrefix = "Bearer ";
    public const string ProblemJsonContentType = "application/problem+json";

    #endregion
}
