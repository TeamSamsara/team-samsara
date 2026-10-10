// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/AccountDeletion/AccountPurgeResult.cs
// Version : 1.0.0
// Latest commit: feat/account-purge
// Author : Gerrah
// Purpose : What one purge run reports: how many expired accounts it removed, skipped and failed to remove.

namespace TeamSamsara.Modules.Identity.Models;

public record AccountPurgeResult(int Purged, int Skipped, int Failed);
