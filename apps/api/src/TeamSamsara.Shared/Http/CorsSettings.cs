// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Http/CorsSettings.cs
// Version : 1.0.0
// Latest commit: feature/api-host
// Author : Gerrah
// Purpose : Binds the allowed CORS origins from configuration. Differs per
// environment purely through appsettings values — localhost locally, real
// Firebase Hosting domains in staging/production — no code branching.

using System.ComponentModel.DataAnnotations;

namespace TeamSamsara.Shared.Http;

public class CorsSettings
{
    #region Properties

    [Required]
    [MinLength(1)]
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

    #endregion
}
