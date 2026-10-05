// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Http/ApiKeyEndpointFilter.cs
// Version : 1.0.0
// Latest commit: feature/assets-module
// Author : Gerrah
// Purpose : Rejects requests to a protected endpoint unless they carry the configured
// API key. Works against any module's settings type via IApiKeySettings, so it's
// reused rather than reimplemented per module.

using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace TeamSamsara.Shared.Http;

public class ApiKeyEndpointFilter<TSettings> : IEndpointFilter
    where TSettings : class, IApiKeySettings
{
    #region Fields

    private const string ApiKeyHeaderName = "X-Api-Key";

    private readonly IOptions<TSettings> _settings;
    private readonly ILogger<ApiKeyEndpointFilter<TSettings>> _logger;

    #endregion

    #region Constructors

    public ApiKeyEndpointFilter(IOptions<TSettings> settings, ILogger<ApiKeyEndpointFilter<TSettings>> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var providedKey = context.HttpContext.Request.Headers[ApiKeyHeaderName].ToString();

        if (string.IsNullOrEmpty(providedKey))
        {
            _logger.LogWarning(
                "Rejected {Path}: missing {HeaderName} header.",
                context.HttpContext.Request.Path, ApiKeyHeaderName);

            return HttpResults.Unauthorized();
        }

        if (!IsValidKey(providedKey))
        {
            _logger.LogWarning(
                "Rejected {Path}: invalid {HeaderName} header.",
                context.HttpContext.Request.Path, ApiKeyHeaderName);

            return HttpResults.Unauthorized();
        }

        return await next(context);
    }

    #endregion

    #region Private Methods

    // Compares the provided key against the configured one in fixed time, so a mismatch
    // can't be guessed one byte at a time from how long the comparison takes.
    private bool IsValidKey(string providedKey)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(_settings.Value.ApiKey);
        var providedBytes = Encoding.UTF8.GetBytes(providedKey);

        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }

    #endregion
}
