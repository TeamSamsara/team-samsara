// File : /team-samsara/apps/api/src/TeamSamsara.Modules.PingPong.Tests/PingPongEndpointsTests.cs
// Version : 1.1.0
// Latest commit: chore/api-contract-baseline
// Author : Gerrah
// Purpose : Proves PingPongModule works end to end against a real running
// instance of the app. GET /ping is open and returns 200/"pong". GET
// /ping/secure requires authorization - with no credentials supplied, it
// must return 401. This is the first automated confirmation of the full
// chain built in Shared.Http and Shared.Authorization. It also proves the
// CORS policy exposes Retry-After and X-Correlation-Id to allowed origins.

using System.Net;
using Shouldly;
using TeamSamsara.Shared.Http;
using TeamSamsara.Shared.Testing;

namespace TeamSamsara.Modules.PingPong.Tests;

public class PingPongEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    #region Fields

    private const string AllowedOrigin = "http://localhost:5173";
    private const string OriginHeader = "Origin";
    private const string ExposeHeadersHeader = "Access-Control-Expose-Headers";

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

    [Fact]
    public async Task Response_FromAllowedOrigin_ExposesRetryAfterAndCorrelationId()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, PingPongConstants.PingRoute);
        request.Headers.Add(OriginHeader, AllowedOrigin);

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var exposed = string.Join(",", response.Headers.GetValues(ExposeHeadersHeader));
        exposed.ShouldContain(HttpConstants.RetryAfterHeader);
        exposed.ShouldContain(HttpConstants.CorrelationIdHeader);
    }

    #endregion
}
