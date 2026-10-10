// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Alert/AlertType.cs
// Version : 1.2.0
// Latest commit: feat/email-change-foundation
// Author : Gerrah
// Purpose : Every system-triggered alert the platform can send, requestable by type from any module.

namespace TeamSamsara.Shared.Alert;

public enum AlertType
{
    // Completes a new account's registration
    RegistrationCode,

    // Confirms a sensitive operation before it is carried out
    StepUpCode,

    // Reports a login attempt from an unrecognized device or IP
    NewLoginAttempt,

    // Reports a password reset requested from an unrecognized device or IP
    PasswordResetRequested,

    // Reports that a password was changed or reset
    PasswordChanged,

    // Reports to the old address that the account email was changed
    EmailChanged
}
