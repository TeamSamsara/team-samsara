// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/Profiles/HandleClaimStatus.cs
// Version : 1.1.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : The outcome of trying to give a handle to a member at the storage
// level. The profile service turns these into profile statuses.

namespace TeamSamsara.Modules.Identity.Models;

public enum HandleClaimStatus
{
    // The member now holds the handle
    Success,

    // Another member already holds the handle
    HandleTaken,

    // The member has no profile to record the handle on
    ProfileNotFound,

    // The member changed their handle too recently to change it again
    HandleCooldown
}
