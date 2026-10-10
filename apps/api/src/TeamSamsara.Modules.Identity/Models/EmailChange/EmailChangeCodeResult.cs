// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/EmailChange/EmailChangeCodeResult.cs
// Version : 1.0.0
// Latest commit: feat/email-change-service
// Author : Gerrah
// Purpose : What requesting an email change returns: the outcome, the code length and the resend wait.

namespace TeamSamsara.Modules.Identity.Models;

public record EmailChangeCodeResult(
    EmailChangeStatus Status,
    int CodeLength,
    int RetryAfterSeconds);
