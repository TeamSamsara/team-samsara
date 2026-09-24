// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Http/CorsSettings.cs
// Version : 1.0.1
// Latest commit: feature/string-magic-value-conventions
// Author : Gerrah
// Purpose : Binds the allowed CORS origins from configuration. Differs per
// environment purely through appsettings values - localhost locally, real
// Firebase Hosting domains in staging/production - no code branching.

using System.ComponentModel.DataAnnotations;

namespace TeamSamsara.Shared.Http;

public class CorsSettings
{
    #region Fields

    public const string SectionName = "Cors";
    public const string DefaultPolicyName = "Default";

    #endregion

    #region Properties

    [Required]
    [MinLength(1)]
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

    #endregion
}
