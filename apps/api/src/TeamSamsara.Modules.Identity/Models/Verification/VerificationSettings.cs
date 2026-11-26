// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/Verification/VerificationSettings.cs
// Version : 1.3.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Configuration for verification codes: lengths, expiry and attempt limits.

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

    [AllowedValues(4, 6, 8, 10)]
    public int PasswordResetCodeLength { get; set; } = 8;

    // Length of both codes sent during an email change
    [AllowedValues(4, 6, 8, 10)]
    public int EmailChangeCodeLength { get; set; } = 8;

    // Minutes a code stays valid
    [Range(1, 60)]
    public int ExpiryMinutes { get; set; } = 10;

    // Wrong guesses allowed before the code is burned
    [Range(1, 10)]
    public int MaxFailedAttempts { get; set; } = 5;

    // Minimum wait before another code can be sent for the same member and purpose
    [Range(0, 600)]
    public int ResendCooldownSeconds { get; set; } = 60;

    // Email change requests one member may make per window, however they end. Since a
    // refused address tells the member it is taken, this limits how fast they can probe.
    [Range(1, 100)]
    public int EmailChangeRequestLimit { get; set; } = 10;

    // Length of that window, in minutes
    [Range(1, 1440)]
    public int EmailChangeRequestWindowMinutes { get; set; } = 60;

    #endregion
}
