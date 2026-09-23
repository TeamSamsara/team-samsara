// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Context/AccessLevel.cs
// Version : 1.0.0
// Latest commit: feature/shared-core-primitives
// Author : Gerrah

// Purpose : The coarse access tier every user is assigned.


namespace TeamSamsara.Shared.Context;

public enum AccessLevel
{
    Guest,
    Member,
    Admin
}
