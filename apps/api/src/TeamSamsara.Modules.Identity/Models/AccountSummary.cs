// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/AccountSummary.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : Who the API considers the caller to be right now. The access level is the
// effective one: a Member whose sign-in is not verified yet is reported as a Guest.

using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Models;

public record AccountSummary(string UserId, AccessLevel AccessLevel);
