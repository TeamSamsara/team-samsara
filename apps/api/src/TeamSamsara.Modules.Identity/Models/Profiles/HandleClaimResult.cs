// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/Profiles/HandleClaimResult.cs
// Version : 1.0.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : What the handle store reports after a claim: the outcome and, on success, the
// member's updated profile so the caller does not have to read it again.

namespace TeamSamsara.Modules.Identity.Models;

public record HandleClaimResult(HandleClaimStatus Status, Profile? Profile);
