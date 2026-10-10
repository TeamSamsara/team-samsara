// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/VerificationIssueStatus.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : The outcome of asking for a verification code to be sent.

namespace TeamSamsara.Modules.Identity.Models;

public enum VerificationIssueStatus
{
    // A new code was generated and sent
    Sent,

    // A code was sent too recently; nothing was sent this time
    CooldownActive
}
