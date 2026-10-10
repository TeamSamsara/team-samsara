// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Alert/AlertTemplateKeys.cs
// Version : 1.1.0
// Latest commit: feat/email-change-foundation
// Author : Gerrah
// Purpose : Names of the template data entries callers supply to IAlertSender.

namespace TeamSamsara.Shared.Alert;

public static class AlertTemplateKeys
{
    #region Fields

    // The one-time verification code (RegistrationCode, StepUpCode)
    public const string Code = "code";

    // The IP address of the request (NewLoginAttempt, PasswordResetRequested)
    public const string IpAddress = "ipAddress";

    // The user agent of the request (NewLoginAttempt, PasswordResetRequested)
    public const string UserAgent = "userAgent";

    // The new email address, masked (EmailChanged)
    public const string NewEmail = "newEmail";

    #endregion
}
