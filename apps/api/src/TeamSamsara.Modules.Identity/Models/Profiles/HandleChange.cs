// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/Profiles/HandleChange.cs
// Version : 1.0.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : A member's own request to change their handle: when it happens and how long
// they must wait between changes. The store applies the rule where it reads the profile.

namespace TeamSamsara.Modules.Identity.Models;

public record HandleChange(DateTimeOffset Now, int CooldownDays);
