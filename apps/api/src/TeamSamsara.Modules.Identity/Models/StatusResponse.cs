// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Models/StatusResponse.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : The body returned by confirm endpoints: just the outcome. Serialized with the
// shared JSON convention, so the status reaches the client as a camelCase string-enum.

namespace TeamSamsara.Modules.Identity.Models;

public record StatusResponse<TStatus>(TStatus Status)
    where TStatus : struct, Enum;
