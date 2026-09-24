// File : /team-samsara/apps/api/src/TeamSamsara.Modules.PingPong.Tests/PingPongEndpointsTests.cs
// Version : 1.0.1
// Latest commit: feature/string-magic-value-conventions
// Author : Gerrah
// Purpose : Proves PingPongModule works end to end against a real running
// instance of the app. GET /ping is open and returns 200/"pong". GET
// /ping/secure requires authorization - with no credentials supplied, it
// must return 401. This is the first automated confirmation of the full
// chain built in Shared.Http and Shared.Authorization.

using System.Net;
using Shouldly;
using TeamSamsara.Shared.Testing;

namespace TeamSamsara.Modules.PingPong.Tests;

public class PingPongEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    #region Fields

    private readonly HttpClient _client;

    #endregion

    #region Constructors

    public PingPongEndpointsTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    #endregion

    #region Public Methods

    [Fact]
    public async Task Ping_ReturnsOkWithPong()
    {
        var response = await _client.GetAsync(PingPongConstants.PingRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.ShouldBe(PingPongConstants.PongResponse);
    }

    [Fact]
    public async Task PingSecure_WithNoCredentials_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync(PingPongConstants.PingSecureRoute);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion
}
