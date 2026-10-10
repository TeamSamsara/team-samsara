// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Http/RateLimitSettings.cs
// Version : 1.0.0
// Latest commit: feat/password-reset-endpoints
// Author : Gerrah
// Purpose : Per-IP request limit for endpoints that signed-out callers can reach.

using System.ComponentModel.DataAnnotations;

namespace TeamSamsara.Shared.Http;

public class RateLimitSettings
{
    #region Fields

    public const string SectionName = "RateLimit";
    public const string PerIpPolicyName = "PerIp";

    #endregion

    #region Properties

    // Requests one IP address may make per window
    [Range(1, 10000)]
    public int PermitLimit { get; set; } = 10;

    // Length of the window, in seconds
    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;

    #endregion
}
