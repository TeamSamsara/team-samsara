// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/VerificationPurpose.cs
// Version : 1.2.0
// Latest commit: feat/email-change-foundation
// Author : Gerrah
// Purpose : The reasons a verification code can be issued, each with at most one active code per member.

namespace TeamSamsara.Modules.Identity.Models;

public enum VerificationPurpose
{
    // Completes a new account's registration
    Registration,

    // Confirms a sensitive operation by a signed-in member
    StepUp,

    // Confirms a login from an unrecognized device or IP
    LoginChallenge,

    // Proves ownership of the email before a forgotten password is replaced
    PasswordReset,

    // Proves the member controls the current address during an email change
    EmailChangeOld,

    // Proves the member controls the new address during an email change
    EmailChangeNew
}
