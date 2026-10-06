// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/MemberOnlyEndpointFilterTests.cs
// Version : 1.0.0
// Latest commit: feature/identity-profile
// Author : Gerrah
// Purpose : Proves the member-only filter: anonymous callers get 401, unverified callers get
// 403 with the reason, and only verified members reach the endpoint.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Shouldly;
using TeamSamsara.Shared.Context;
using TeamSamsara.Shared.Http;

namespace TeamSamsara.Modules.Identity.Tests;

public class MemberOnlyEndpointFilterTests
{
    #region Fields

    private const string EndpointResult = "endpoint-result";

    private bool _endpointWasReached;

    #endregion

    #region Public Methods

    [Fact]
    public async Task AnAnonymousCaller_GetsUnauthorized_AndNeverReachesTheEndpoint()
    {
        var result = await RunAsync(AuthState.Anonymous);

        result.ShouldBeOfType<UnauthorizedHttpResult>();
        _endpointWasReached.ShouldBeFalse();
    }

    [Theory]
    [InlineData(AuthState.RegistrationIncomplete)]
    [InlineData(AuthState.VerificationRequired)]
    public async Task AnUnverifiedCaller_GetsForbiddenWithTheReason_AndNeverReachesTheEndpoint(
        AuthState authState)
    {
        var result = await RunAsync(authState);

        var forbidden = result.ShouldBeOfType<JsonHttpResult<AuthStateResponse>>();
        forbidden.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        forbidden.Value.ShouldNotBeNull().AuthState.ShouldBe(authState);
        _endpointWasReached.ShouldBeFalse();
    }

    [Fact]
    public async Task AVerifiedMember_ReachesTheEndpoint()
    {
        var result = await RunAsync(AuthState.Member);

        result.ShouldBe(EndpointResult);
        _endpointWasReached.ShouldBeTrue();
    }

    #endregion

    #region Private Methods

    // Runs the filter for a caller in the given state; the endpoint only records that it ran
    private async Task<object?> RunAsync(AuthState authState)
    {
        var filter = new MemberOnlyEndpointFilter(new CurrentUserContext { AuthState = authState });
        var context = EndpointFilterInvocationContext.Create(new DefaultHttpContext());

        return await filter.InvokeAsync(context, _ =>
        {
            _endpointWasReached = true;

            return ValueTask.FromResult<object?>(EndpointResult);
        });
    }

    #endregion
}
