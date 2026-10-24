// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/ProfileHandleEndpointsTests.cs
// Version : 1.1.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Proves the handle, onboarding and public profile endpoints: who may call them, how outcomes map to status codes, and the public rate limit.

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Modules.Identity.Endpoints;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Tests.Fakes;
using TeamSamsara.Shared.Context;
using TeamSamsara.Shared.Http;

namespace TeamSamsara.Modules.Identity.Tests;

public class ProfileHandleEndpointsTests
{
    #region Fields

    private const string UserId = "user-1";
    private const string OtherUserId = "user-2";
    private const string AvailablePath = "/profile/handle/available";
    private const string HandlePath = "/profile/handle";
    private const string OnboardingPath = "/profile/onboarding/complete";
    private const string PublicPath = "/profiles/";
    private const int PublicPermitLimit = 2;
    private const string ClientIpHeader = "X-Test-Ip";

    private readonly InMemoryProfileStore _profiles = new();
    private readonly InMemoryHandleStore _handles;
    private readonly FakeClock _clock = new();

    #endregion

    #region Constructors

    public ProfileHandleEndpointsTests()
    {
        _handles = new InMemoryHandleStore(_profiles);
    }

    #endregion

    #region Public Methods

    [Fact]
    public async Task AnAnonymousCaller_CannotCheckOrChangeAHandle()
    {
        await using var app = await StartAsync(AuthState.Anonymous);

        (await app.Client.GetAsync($"{AvailablePath}?handle=freename")).StatusCode
            .ShouldBe(HttpStatusCode.Unauthorized);
        (await app.Client.PutAsJsonAsync(HandlePath, new SetHandleRequest("freename"))).StatusCode
            .ShouldBe(HttpStatusCode.Unauthorized);
        (await app.Client.PostAsync(OnboardingPath, content: null)).StatusCode
            .ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AnUnverifiedCaller_GetsForbidden()
    {
        await using var app = await StartAsync(AuthState.VerificationRequired);

        (await app.Client.GetAsync($"{AvailablePath}?handle=freename")).StatusCode
            .ShouldBe(HttpStatusCode.Forbidden);
        (await app.Client.PostAsync(OnboardingPath, content: null)).StatusCode
            .ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("freename", HttpStatusCode.OK)]
    [InlineData("taken", HttpStatusCode.Conflict)]
    [InlineData("admin", HttpStatusCode.Conflict)]
    [InlineData("no", HttpStatusCode.BadRequest)]
    public async Task TheAvailabilityCheck_MapsTheOutcomeToAStatusCode(string handle, HttpStatusCode expected)
    {
        await using var app = await StartAsync(AuthState.Member);
        await _handles.ClaimAsync(OtherUserId, "taken", null);

        var response = await app.Client.GetAsync($"{AvailablePath}?handle={handle}");

        response.StatusCode.ShouldBe(expected);
    }

    [Fact]
    public async Task TheAvailabilityCheck_WithoutAHandle_GetsBadRequest()
    {
        await using var app = await StartAsync(AuthState.Member);

        var response = await app.Client.GetAsync(AvailablePath);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AMember_ChangingTheirHandle_GetsOk_AndTheHandleIsSaved()
    {
        await using var app = await StartAsync(AuthState.Member);

        var response = await app.Client.PutAsJsonAsync(HandlePath, new SetHandleRequest("freename"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await _handles.GetOwnerAsync("freename")).ShouldBe(UserId);
    }

    [Theory]
    [InlineData("taken", HttpStatusCode.Conflict)]
    [InlineData("no", HttpStatusCode.BadRequest)]
    public async Task AMember_ChoosingARefusedHandle_GetsTheMatchingStatusCode(string handle, HttpStatusCode expected)
    {
        await using var app = await StartAsync(AuthState.Member);
        await _handles.ClaimAsync(OtherUserId, "taken", null);

        var response = await app.Client.PutAsJsonAsync(HandlePath, new SetHandleRequest(handle));

        response.StatusCode.ShouldBe(expected);
    }

    [Fact]
    public async Task AMember_ChangingTheirHandleTwiceInARow_IsTold429()
    {
        await using var app = await StartAsync(AuthState.Member);
        await _profiles.ModifyAsync(UserId, profile => profile.OnboardingCompleted = true);
        await app.Client.PutAsJsonAsync(HandlePath, new SetHandleRequest("firstname"));

        var response = await app.Client.PutAsJsonAsync(HandlePath, new SetHandleRequest("secondname"));

        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task AMember_CompletingOnboarding_GetsOk_AndTheFlagIsSaved()
    {
        await using var app = await StartAsync(AuthState.Member);

        var response = await app.Client.PostAsync(OnboardingPath, content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await _profiles.GetByIdAsync(UserId))!.OnboardingCompleted.ShouldBeTrue();
    }

    [Fact]
    public async Task APublicLookup_NeedsNoSignIn_AndReturnsTheProfileWithoutAnId()
    {
        await using var app = await StartAsync(AuthState.Anonymous);
        await _handles.ClaimAsync(OtherUserId, "taken", null);

        var response = await app.Client.GetAsync($"{PublicPath}Taken");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("taken");
        body.ShouldNotContain(OtherUserId);
    }

    [Fact]
    public async Task APublicLookup_OfAnUnknownHandle_GetsNotFound()
    {
        await using var app = await StartAsync(AuthState.Anonymous);

        var response = await app.Client.GetAsync($"{PublicPath}nobody");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task APublicLookup_IsRateLimitedPerIp()
    {
        await using var app = await StartAsync(AuthState.Anonymous);

        for (var i = 0; i < PublicPermitLimit; i++)
        {
            (await GetFromAsync(app, "10.0.0.1")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        (await GetFromAsync(app, "10.0.0.1")).StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        (await GetFromAsync(app, "10.0.0.2")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region Private Methods

    private static Task<HttpResponseMessage> GetFromAsync(TestApp app, string ip)
    {
        var message = new HttpRequestMessage(HttpMethod.Get, $"{PublicPath}nobody");
        message.Headers.Add(ClientIpHeader, ip);

        return app.Client.SendAsync(message);
    }

    private async Task<TestApp> StartAsync(AuthState authState)
    {
        await _profiles.CreateAsync(new Profile { Id = UserId, DisplayName = "Member" });
        await _profiles.CreateAsync(new Profile { Id = OtherUserId, DisplayName = "Other" });

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<ICurrentUserContext>(new CurrentUserContext
        {
            IsAuthenticated = authState != AuthState.Anonymous,
            UserId = authState == AuthState.Anonymous ? null : UserId,
            AuthState = authState
        });
        builder.Services.AddSingleton<IProfileService>(new ProfileService(
            _profiles,
            _handles,
            new FakeMemberImageService(),
            Options.Create(new ProfileSettings()),
            _clock,
            NullLogger<ProfileService>.Instance));
        builder.Services.AddSamsaraRateLimiting(new RateLimitSettings());
        builder.Services.AddPerIpRateLimitPolicy(PublicProfileEndpoints.RateLimitPolicyName, PublicPermitLimit, 60);

        var app = builder.Build();

        // The test server has no remote address, so the tests choose one per request.
        app.Use((context, next) =>
        {
            if (context.Request.Headers.TryGetValue(ClientIpHeader, out var ip))
            {
                context.Connection.RemoteIpAddress = IPAddress.Parse(ip.ToString());
            }

            return next();
        });

        app.UseRouting();
        app.UseRateLimiter();
        ProfileEndpoints.Map(app);
        PublicProfileEndpoints.Map(app);
        await app.StartAsync();

        var server = (TestServer)app.Services.GetRequiredService<IServer>();
        return new TestApp(app, server.CreateClient());
    }

    #endregion

    #region Nested Types

    private sealed class TestApp(WebApplication app, HttpClient client) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
        }
    }

    #endregion
}
