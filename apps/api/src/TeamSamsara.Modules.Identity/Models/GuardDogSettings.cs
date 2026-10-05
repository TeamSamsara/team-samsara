// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/GuardDogSettings.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : Configuration for GuardDog, the client fingerprinting service.

using System.ComponentModel.DataAnnotations;

namespace TeamSamsara.Modules.Identity.Models;

public class GuardDogSettings
{
    #region Fields

    public const string SectionName = "Identity:GuardDog";

    #endregion

    #region Properties

    // Server-side secret used to key the fingerprint hash, so stored fingerprints cannot be
    // reversed by brute-forcing IP addresses
    [Required]
    [MinLength(16)]
    public string Secret { get; set; } = string.Empty;

    // How many known fingerprints are kept per member; the least recently seen are dropped
    [Range(1, 50)]
    public int MaxKnownFingerprints { get; set; } = 10;

    #endregion
}
