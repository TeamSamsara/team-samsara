// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Http/SecurityHeadersMiddleware.cs
// Version : 1.0.2
// Latest commit: feature/string-magic-value-conventions
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
        context.Response.Headers.XContentTypeOptions = SecurityHeaderValues.NoSniff;
        context.Response.Headers.XFrameOptions = SecurityHeaderValues.Deny;
        context.Response.Headers[SecurityHeaderValues.ReferrerPolicyHeaderName] = SecurityHeaderValues.StrictOriginWhenCrossOrigin;

        return _next(context);
    }

    #endregion
}
