// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/VerificationPurpose.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : The reasons a verification code can be issued. Each purpose has its own code
// length and at most one active code per member.

namespace TeamSamsara.Modules.Identity.Models;

public enum VerificationPurpose
{
    // Completing a new account's registration
    Registration,

    // Confirming a sensitive operation (email change, password change, account deletion,
    // password reset)
    StepUp,

    // Confirming a login from an unrecognized device or IP
    LoginChallenge
}
