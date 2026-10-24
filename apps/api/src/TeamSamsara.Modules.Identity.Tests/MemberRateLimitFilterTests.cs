// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/MemberRateLimitFilterTests.cs
// Version : 1.0.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Proves the per-member rate limit filter: calls within the limit get through, the
// next one gets 429 with Retry-After, members are counted separately, and a caller who is not
// signed in is left alone.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TeamSamsara.Shared.Context;
using TeamSamsara.Shared.Http;

namespace TeamSamsara.Modules.Identity.Tests;

public class MemberRateLimitFilterTests
{
    #region Fields

    private const string EndpointResult = "endpoint-result";
    private const int Limit = 3;
    private const int WindowSeconds = 60;

    private readonly MemberRateLimitFilter _filter = new(Limit, WindowSeconds);

    #endregion

    #region Public Methods

    [Fact]
    public async Task CallsWithinTheLimit_ReachTheEndpoint()
    {
        for (var call = 0; call < Limit; call++)
        {
            var (result, _) = await RunAsync("user-1");

            result.ShouldBe(EndpointResult);
        }
    }

    [Fact]
    public async Task TheCallBeyondTheLimit_GetsTooManyRequests_WithRetryAfter()
    {
        await UseUpTheLimitAsync("user-1");

        var (result, context) = await RunAsync("user-1");

        result.ShouldBeOfType<StatusCodeHttpResult>().StatusCode.ShouldBe(StatusCodes.Status429TooManyRequests);
        context.Response.Headers.RetryAfter.ToString().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task EachMember_IsCountedSeparately()
    {
        await UseUpTheLimitAsync("user-1");

        var (result, _) = await RunAsync("user-2");

        result.ShouldBe(EndpointResult);
    }

    [Fact]
    public async Task ACallerWhoIsNotSignedIn_IsNeverLimited()
    {
        for (var call = 0; call < Limit * 2; call++)
        {
            var (result, _) = await RunAsync(null);

            result.ShouldBe(EndpointResult);
        }
    }

    #endregion

    #region Private Methods

    // Makes as many calls as the limit allows for this member
    private async Task UseUpTheLimitAsync(string userId)
    {
        for (var call = 0; call < Limit; call++)
        {
            await RunAsync(userId);
        }
    }

    // Runs the filter for a caller; a null user id is a caller who is not signed in
    private async Task<(object? Result, HttpContext Context)> RunAsync(string? userId)
    {
        var services = new ServiceCollection()
            .AddSingleton<ICurrentUserContext>(new CurrentUserContext { UserId = userId })
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = services };
        var context = EndpointFilterInvocationContext.Create(httpContext);

        var result = await _filter.InvokeAsync(
            context,
            _ => ValueTask.FromResult<object?>(EndpointResult));

        return (result, httpContext);
    }

    #endregion
}
