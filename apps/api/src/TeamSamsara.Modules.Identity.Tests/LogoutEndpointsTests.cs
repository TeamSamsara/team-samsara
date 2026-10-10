// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/LogoutEndpointsTests.cs
// Version : 1.0.0
// Latest commit: feat/logout
// Author : Gerrah
// Purpose : Proves the logout endpoints are for verified members only and end one sign-in or every sign-in as asked.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Modules.Identity.Endpoints;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Tests.Fakes;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Tests;

public class LogoutEndpointsTests
{
    #region Fields

    private const string UserId = "user-1";
    private const string Email = "jane.doe@example.com";
    private const string LogoutPath = "/logout";
    private const string LogoutAllPath = "/logout/all";
    private const long ThisDevice = 1_700_000_000;
    private const long OtherDevice = 1_700_000_500;

    private readonly InMemoryUserStore _users = new();
    private readonly InMemoryCacheService _cache = new();
    private readonly FakeAccountGateway _accounts = new();
    private readonly FakeClock _clock = new();
    private SignInTracker _signIns = null!;

    #endregion

    #region Public Methods

    [Theory]
    [InlineData(LogoutPath)]
    [InlineData(LogoutAllPath)]
    public async Task AnAnonymousCaller_GetsUnauthorized(string path)
    {
        await using var app = await StartAsync(AuthState.Anonymous);

        var response = await app.Client.PostAsync(path, content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(LogoutPath, AuthState.RegistrationIncomplete)]
    [InlineData(LogoutPath, AuthState.VerificationRequired)]
    [InlineData(LogoutAllPath, AuthState.RegistrationIncomplete)]
    [InlineData(LogoutAllPath, AuthState.VerificationRequired)]
    public async Task AnUnverifiedCaller_GetsForbidden_AndNothingIsChanged(string path, AuthState authState)
    {
        await using var app = await StartAsync(authState);

        var response = await app.Client.PostAsync(path, content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await _signIns.IsVerifiedAsync(UserId, ThisDevice)).ShouldBeTrue();
        (await _signIns.IsVerifiedAsync(UserId, OtherDevice)).ShouldBeTrue();
        _accounts.RevokedSessions.ShouldBeEmpty();
    }

    [Fact]
    public async Task Logout_ForAMember_GetsNoContent_AndEndsOnlyThisSignIn()
    {
        await using var app = await StartAsync(AuthState.Member);

        var response = await app.Client.PostAsync(LogoutPath, content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _signIns.IsVerifiedAsync(UserId, ThisDevice)).ShouldBeFalse();
        (await _signIns.IsVerifiedAsync(UserId, OtherDevice)).ShouldBeTrue();
        _accounts.RevokedSessions.ShouldBeEmpty();
    }

    [Fact]
    public async Task Logout_Twice_GetsNoContentBothTimes()
    {
        await using var app = await StartAsync(AuthState.Member);

        var first = await app.Client.PostAsync(LogoutPath, content: null);
        var second = await app.Client.PostAsync(LogoutPath, content: null);

        first.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        second.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Logout_WithoutAReadableSignInTime_GetsUnauthorized_AndChangesNothing()
    {
        await using var app = await StartAsync(AuthState.Member, authTime: null);

        var response = await app.Client.PostAsync(LogoutPath, content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await _signIns.IsVerifiedAsync(UserId, ThisDevice)).ShouldBeTrue();
    }

    [Fact]
    public async Task LogoutAll_ForAMember_GetsNoContent_AndEndsEverySignIn()
    {
        await using var app = await StartAsync(AuthState.Member);

        var response = await app.Client.PostAsync(LogoutAllPath, content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _signIns.IsVerifiedAsync(UserId, ThisDevice)).ShouldBeFalse();
        (await _signIns.IsVerifiedAsync(UserId, OtherDevice)).ShouldBeFalse();
        _accounts.RevokedSessions.ShouldHaveSingleItem().ShouldBe(UserId);
    }

    #endregion

    #region Private Methods

    // Builds the endpoints on a test server with the real service and in-memory fakes.
    private async Task<TestApp> StartAsync(AuthState authState, long? authTime = ThisDevice)
    {
        _users.Seed(new User
        {
            Id = UserId,
            AccessLevel = AccessLevel.Member,
            CreatedAt = _clock.UtcNow,
            VerifiedSignIns = new List<long> { ThisDevice, OtherDevice }
        });
        _accounts.AddAccount(UserId, Email);

        _signIns = new SignInTracker(_users, _cache, Options.Create(new AuthenticationSettings()));
        var sessions = new SessionService(_signIns, _accounts);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<ICurrentUserContext>(new CurrentUserContext
        {
            IsAuthenticated = authState != AuthState.Anonymous,
            UserId = authState == AuthState.Anonymous ? null : UserId,
            AuthState = authState,
            AuthTime = authTime
        });
        builder.Services.AddSingleton<ISessionService>(sessions);

        var app = builder.Build();
        LogoutEndpoints.Map(app);
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
