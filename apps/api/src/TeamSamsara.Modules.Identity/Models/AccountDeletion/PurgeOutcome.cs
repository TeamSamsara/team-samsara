// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/AccountDeletion/PurgeOutcome.cs
// Version : 1.0.0
// Latest commit: feat/notification-erasure
// Author : Gerrah
// Purpose : How one account fared in a purge run.

namespace TeamSamsara.Modules.Identity.Models;

internal enum PurgeOutcome
{
    // The account and everything of it was removed
    Purged,

    // The account was restored, or another instance holds it, so nothing was done
    Skipped,

    // Removal stopped on an error and the account will be retried on the next run
    Failed
}
