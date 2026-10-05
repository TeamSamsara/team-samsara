// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Http/SecurityHeaderValues.cs
// Version : 1.0.0
// Latest commit: feature/string-magic-value-conventions
// Author : Gerrah
// Purpose : Security header names/values our code depends on matching exactly.

namespace TeamSamsara.Shared.Http;

public static class SecurityHeaderValues
{
    #region Fields

    public const string NoSniff = "nosniff";
    public const string Deny = "DENY";
    public const string ReferrerPolicyHeaderName = "Referrer-Policy";
    public const string StrictOriginWhenCrossOrigin = "strict-origin-when-cross-origin";

    #endregion
}
