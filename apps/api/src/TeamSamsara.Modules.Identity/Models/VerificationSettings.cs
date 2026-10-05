// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/VerificationSettings.cs
// Version : 1.0.1
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : Configuration for verification codes. The server alone decides code length and
// limits; clients are told the length in the response, never asked for it.

using System.ComponentModel.DataAnnotations;

namespace TeamSamsara.Modules.Identity.Models;

public class VerificationSettings
{
    #region Fields

    public const string SectionName = "Identity:Verification";

    #endregion

    #region Properties

    [AllowedValues(4, 6, 8, 10)]
    public int RegistrationCodeLength { get; set; } = 6;

    [AllowedValues(4, 6, 8, 10)]
    public int StepUpCodeLength { get; set; } = 8;

    [AllowedValues(4, 6, 8, 10)]
    public int LoginChallengeCodeLength { get; set; } = 6;

    // How long a code stays valid
    [Range(1, 60)]
    public int ExpiryMinutes { get; set; } = 10;

    // Wrong guesses allowed before the code is burned
    [Range(1, 10)]
    public int MaxFailedAttempts { get; set; } = 5;

    // Minimum wait before another code can be sent for the same member and purpose, so the
    // send endpoint cannot be used to flood an inbox
    [Range(0, 600)]
    public int ResendCooldownSeconds { get; set; } = 60;

    #endregion
}
