// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Contracts/PagedResponse.cs
// Version : 1.0.0
// Latest commit: feature/shared-core-primitives
// Author : Gerrah

// Purpose : Standard output shape for paginated list endpoints.

using System;
using System.Collections.Generic;

namespace TeamSamsara.Shared.Contracts;

public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount)
{
    #region Properties

    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    #endregion
}
