// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Http/CorrelationIdMiddleware.cs
// Version : 1.1.0
// Latest commit: chore/api-contract-baseline
// Author : Gerrah
// Purpose : Attaches a correlation ID to each request for distributed request tracing.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace TeamSamsara.Shared.Http;

public class CorrelationIdMiddleware
{
    #region Fields

    private readonly RequestDelegate _next;

    #endregion

    #region Constructors

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    #endregion

    #region Public Methods

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HttpConstants.CorrelationIdHeader, out var incoming)
            ? incoming.ToString()
            : Guid.NewGuid().ToString();

        context.Response.Headers[HttpConstants.CorrelationIdHeader] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    #endregion
}
