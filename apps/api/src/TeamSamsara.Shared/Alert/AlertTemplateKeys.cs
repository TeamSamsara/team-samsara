// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Alert/AlertTemplateKeys.cs
// Version : 1.0.1
// Latest commit: feat/alert-password-notices
// Author : Gerrah
// Purpose : Names of the template data entries callers supply to IAlertSender. Shared so that
// callers and the Alert module's templates agree on the exact keys without magic strings.

namespace TeamSamsara.Shared.Alert;

public static class AlertTemplateKeys
{
    #region Fields

    // The one-time verification code (RegistrationCode, StepUpCode)
    public const string Code = "code";

    // The IP address the request came from (NewLoginAttempt, PasswordResetRequested)
    public const string IpAddress = "ipAddress";

    // The user agent / device description the request came from (NewLoginAttempt,
    // PasswordResetRequested)
    public const string UserAgent = "userAgent";

    #endregion
}
