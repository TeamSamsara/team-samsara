// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Alert/AlertType.cs
// Version : 1.4.0
// Latest commit: task/web-account-security
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
    EmailChanged,

    // Reports that the account was deleted and can be restored by signing in within the recovery window
    AccountDeleted,

    // Confirms a sign-in from an unrecognized device or IP
    LoginCode,

    // Confirms a new password chosen by a signed-in member
    PasswordChangeCode,

    // Proves ownership of the email before a forgotten password is replaced
    PasswordResetCode,

    // Sent to the current address to confirm an email change
    EmailChangeCurrentCode,

    // Sent to the new address to confirm an email change
    EmailChangeNewCode,

    // Confirms the deletion of the account
    AccountDeletionCode
}
