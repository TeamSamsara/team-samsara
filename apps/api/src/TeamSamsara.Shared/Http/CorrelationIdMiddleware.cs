// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Http/CorrelationIdMiddleware.cs
// Version : 1.0.0
// Latest commit: feature/shared-core-primitives
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

    private const string HeaderName = "X-Correlation-Id";
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
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var incoming)
            ? incoming.ToString()
            : Guid.NewGuid().ToString();

        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    #endregion
}
