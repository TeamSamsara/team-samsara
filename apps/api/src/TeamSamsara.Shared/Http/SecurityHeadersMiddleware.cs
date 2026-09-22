// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Http/SecurityHeadersMiddleware.cs
// Version : 1.0.0
// Latest commit: feature/shared-core-primitives
// Author : Gerrah
// Purpose : Appends a fixed set of security-related headers to every response.

using Microsoft.AspNetCore.Http;

namespace TeamSamsara.Shared.Http;

public class SecurityHeadersMiddleware
{
    #region Fields

    private readonly RequestDelegate _next;

    #endregion

    #region Constructors

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    #endregion

    #region Public Methods

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        return _next(context);
    }

    #endregion
}
