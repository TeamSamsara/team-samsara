// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/AccountDeletion/AccountDeletionCodeResult.cs
// Version : 1.0.0
// Latest commit: feat/account-deletion
// Author : Gerrah
// Purpose : What requesting a deletion code returns: the outcome, plus the code length and
// resend wait the client needs to draw its verification screen.

namespace TeamSamsara.Modules.Identity.Models;

public record AccountDeletionCodeResult(
    AccountDeletionStatus Status,
    int CodeLength,
    int RetryAfterSeconds);
