// File : /team-samsara/apps/api/src/TeamSamsara.Shared/Http/ExceptionHandlingMiddleware.cs
// Version : 1.0.1
// Latest commit: feature/string-magic-value-conventions
// Author : Gerrah
// Purpose : Maps unhandled exceptions to standardized ProblemDetails responses.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TeamSamsara.Shared.Results;

namespace TeamSamsara.Shared.Http;

public class ExceptionHandlingMiddleware
{
    #region Fields

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    #endregion

    #region Constructors

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException appException)
        {
            _logger.LogWarning(appException, "Handled application error: {Code}", appException.Error.Code);
            await WriteProblemDetailsAsync(context, appException.Error);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception");
            var error = Error.Failure(ErrorCodes.UnexpectedError, ResultMessages.UnexpectedError);
            await WriteProblemDetailsAsync(context, error);
        }
    }

    #endregion

    #region Private Methods

    private static Task WriteProblemDetailsAsync(HttpContext context, Error error)
    {
        var statusCode = MapToStatusCode(error.Type);

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = error.Code,
            Detail = error.Message
        };

        context.Response.ContentType = HttpConstants.ProblemJsonContentType;
        context.Response.StatusCode = (int)statusCode;

        return context.Response.WriteAsJsonAsync(problemDetails);
    }

    private static HttpStatusCode MapToStatusCode(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => HttpStatusCode.BadRequest,
        ErrorType.Unauthorized => HttpStatusCode.Unauthorized,
        ErrorType.NotFound => HttpStatusCode.NotFound,
        ErrorType.Conflict => HttpStatusCode.Conflict,
        _ => HttpStatusCode.InternalServerError
    };

    #endregion
}
