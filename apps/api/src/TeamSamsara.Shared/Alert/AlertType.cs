// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Alert/AlertType.cs
// Version : 1.0.0
// Latest commit: feature/alerts-module
// Author : Gerrah
// Purpose : Enumerates every system-triggered security/transactional alert the platform can
// send. Lives in Shared so any module can request an alert by type without referencing the
// Alert module directly.

namespace TeamSamsara.Shared.Alert;

public enum AlertType
{
    // Sent when a new account is created; must be confirmed to complete registration
    RegistrationCode,

    // Sent to confirm a sensitive operation (email change, password change, account deletion,
    // password reset) before it is carried out
    StepUpCode,

    // Sent whenever a login is attempted from an unrecognized device/IP fingerprint, regardless
    // of whether the login attempt ultimately succeeds
    NewLoginAttempt
}
