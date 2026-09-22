// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Results/ErrorType.cs
// Version : 1.0.0
// Latest commit: feature/shared-core-primitives
// Author : Gerrah

// Purpose : Categorize an Error so the HTTP layer can map it to the correct
// status code without inspecting Code strings.

namespace TeamSamsara.Shared.Results;

public enum ErrorType
{
    None,
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Failure
}
