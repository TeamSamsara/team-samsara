// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Http/HttpRequestExtensions.cs
// Version : 1.0.0
// Latest commit: feat/password-reset-endpoints
// Author : Gerrah
// Purpose : Reads a JSON request body inside an endpoint handler, so a missing or malformed
// body can be answered with a plain 400.

using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace TeamSamsara.Shared.Http;

public static class HttpRequestExtensions
{
    #region Public Methods

    // Returns null when the request is not JSON or the JSON is malformed. Endpoints behind
    // MemberOnlyEndpointFilter read the body here rather than as a bound parameter, because
    // binding runs before the filter and would turn a 401 or 403 into a 400.
    public static async Task<T?> ReadJsonBodyAsync<T>(
        this HttpRequest request,
        CancellationToken cancellationToken)
        where T : class
    {
        if (!request.HasJsonContentType())
        {
            return null;
        }

        try
        {
            return await request.ReadFromJsonAsync<T>(cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    #endregion
}
