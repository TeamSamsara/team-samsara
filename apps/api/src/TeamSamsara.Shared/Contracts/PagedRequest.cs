// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Contracts/PagedRequest.cs
// Version : 1.0.0
// Latest commit: feature/shared-core-primitives
// Author : Gerrah

// Purpose : Standard input shape for paginated list endpoints. Pages are 1-indexed.

namespace TeamSamsara.Shared.Contracts;

public record PagedRequest(int PageNumber, int PageSize);
