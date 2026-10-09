// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/VerificationPurpose.cs
// Version : 1.1.0
// Latest commit: feat/password-reset-purpose
// Author : Gerrah
// Purpose : The reasons a verification code can be issued. Each purpose has its own code
// length and at most one active code per member.

namespace TeamSamsara.Modules.Identity.Models;

public enum VerificationPurpose
{
    // Completing a new account's registration
    Registration,

    // Confirming a sensitive operation by a signed-in member (email change, password change,
    // account deletion)
    StepUp,

    // Confirming a login from an unrecognized device or IP
    LoginChallenge,

    // Proving ownership of the email address before a forgotten password is replaced. Kept
    // apart from StepUp so a reset request never overwrites or blocks a signed-in member's
    // pending step-up code.
    PasswordReset
}
