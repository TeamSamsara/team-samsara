// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/AccountDeletionEndpointsTests.cs
// Version : 1.0.0
// Latest commit: feat/account-deletion
// Author : Gerrah
// Purpose : Proves the account deletion endpoints are for verified members only, delete on the right code, and leave a deleted member's old sign-in unable to reach member-only endpoints.

using System.Net;
using System.Net.Http.Json;
using System.Text;
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
using TeamSamsara.Shared.Alert;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Tests;

public class AccountDeletionEndpointsTests
{
    #region Fields

    private const string UserId = "user-1";
    private const string Email = "jane.doe@example.com";
    private const string RequestCodePath = "/delete/request-code";
    private const string DeletePath = "/delete";
    private const string WrongCode = "not-the-code";
    private const long ThisDevice = 1_700_000_000;
    private const long OtherDevice = 1_700_000_500;

    private readonly InMemoryUserStore _users = new();
    private readonly InMemoryVerificationCodeStore _codes = new();
    private readonly InMemoryCacheService _cache = new();
    private readonly RecordingAlertSender _alerts = new();
    private readonly FakeAccountGateway _accounts = new();
    private readonly FakeClock _clock = new();
    private readonly SignInTracker _signIns;
    private readonly AccountDeletionService _service;

    #endregion

    #region Constructors

    public AccountDeletionEndpointsTests()
    {
        var verification = new VerificationCodeService(
            _codes, _alerts, _clock, Options.Create(new VerificationSettings()));

        _signIns = new SignInTracker(_users, _cache, Options.Create(new AuthenticationSettings()));

        _service = new AccountDeletionService(
            _users,
            _accounts,
            _signIns,
            verification,
            _alerts,
            _clock,
            Options.Create(new AuthenticationSettings()),
            NullLogger<AccountDeletionService>.Instance);

        _accounts.AddAccount(UserId, Email);
        _users.Seed(new User
        {
            Id = UserId,
            AccessLevel = AccessLevel.Member,
            CreatedAt = _clock.UtcNow,
            VerifiedSignIns = new List<long> { ThisDevice, OtherDevice }
        });
    }

    #endregion

    #region Public Methods

    [Theory]
    [InlineData(RequestCodePath)]
    [InlineData(DeletePath)]
    public async Task AnAnonymousCaller_GetsUnauthorized(string path)
    {
        await using var app = await StartAsync(AuthState.Anonymous);

        var response = await app.Client.PostAsync(path, content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(RequestCodePath, AuthState.RegistrationIncomplete)]
    [InlineData(RequestCodePath, AuthState.VerificationRequired)]
    [InlineData(DeletePath, AuthState.RegistrationIncomplete)]
    [InlineData(DeletePath, AuthState.VerificationRequired)]
    public async Task AnUnverifiedCaller_GetsForbidden_AndNothingIsSentOrChanged(string path, AuthState authState)
    {
        await using var app = await StartAsync(authState);

        var response = await app.Client.PostAsync(path, JsonContent.Create(new DeleteAccountRequest("12345678")));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        _alerts.Sent.ShouldBeEmpty();
        await AssertNotDeletedAsync();
    }

    [Fact]
    public async Task RequestCode_ForAMember_GetsOk_AndTheCodeIsEmailed()
    {
        await using var app = await StartAsync(AuthState.Member);

        var response = await app.Client.PostAsync(RequestCodePath, content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var alert = _alerts.Sent.ShouldHaveSingleItem();
        alert.To.ShouldBe(Email);
        alert.Type.ShouldBe(AlertType.StepUpCode);
    }

    [Fact]
    public async Task RequestCode_AgainTooSoon_GetsTooManyRequests()
    {
        await using var app = await StartAsync(AuthState.Member);
        await app.Client.PostAsync(RequestCodePath, content: null);

        var response = await app.Client.PostAsync(RequestCodePath, content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        _alerts.Sent.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Delete_SendingANonJsonBody_GetsBadRequest()
    {
        await using var app = await StartAsync(AuthState.Member);
        var body = new StringContent("hello", Encoding.UTF8, "text/plain");

        var response = await app.Client.PostAsync(DeletePath, body);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await AssertNotDeletedAsync();
    }

    [Fact]
    public async Task Delete_SendingMalformedJson_GetsBadRequest()
    {
        await using var app = await StartAsync(AuthState.Member);
        var body = new StringContent("{ not json", Encoding.UTF8, "application/json");

        var response = await app.Client.PostAsync(DeletePath, body);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await AssertNotDeletedAsync();
    }

    [Fact]
    public async Task Delete_WithABlankCode_GetsBadRequest()
    {
        await using var app = await StartAsync(AuthState.Member);

        var response = await app.Client.PostAsync(
            DeletePath, JsonContent.Create(new DeleteAccountRequest("  ")));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await AssertNotDeletedAsync();
    }

    [Fact]
    public async Task Delete_WithAWrongCode_GetsBadRequest_AndNothingChanges()
    {
        await using var app = await StartAsync(AuthState.Member);
        await app.Client.PostAsync(RequestCodePath, content: null);

        var response = await app.Client.PostAsync(
            DeletePath, JsonContent.Create(new DeleteAccountRequest(WrongCode)));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await AssertNotDeletedAsync();
    }

    [Fact]
    public async Task Delete_WithTheRightCode_GetsOk_AndDeletesTheAccount()
    {
        await using var app = await StartAsync(AuthState.Member);
        await app.Client.PostAsync(RequestCodePath, content: null);

        var response = await app.Client.PostAsync(
            DeletePath, JsonContent.Create(new DeleteAccountRequest(_alerts.LastCode)));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var stored = await _users.GetByIdAsync(UserId);
        stored.ShouldNotBeNull();
        stored.DeletedAt.ShouldNotBeNull();
        _accounts.RevokedSessions.ShouldHaveSingleItem().ShouldBe(UserId);
    }

    [Fact]
    public async Task Delete_WithTheSameCodeTwice_GetsNotFoundTheSecondTime()
    {
        await using var app = await StartAsync(AuthState.Member);
        await app.Client.PostAsync(RequestCodePath, content: null);
        var body = new DeleteAccountRequest(_alerts.LastCode);

        var first = await app.Client.PostAsync(DeletePath, JsonContent.Create(body));
        var second = await app.Client.PostAsync(DeletePath, JsonContent.Create(body));

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        _accounts.RevokedSessions.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task AfterDeletion_TheSameSignIn_IsNoLongerAMember_AndIsRefusedByMemberOnlyEndpoints()
    {
        await using var before = await StartAsync(AuthState.Member);
        await before.Client.PostAsync(RequestCodePath, content: null);
        await before.Client.PostAsync(
            DeletePath, JsonContent.Create(new DeleteAccountRequest(_alerts.LastCode)));

        // The caller state the Firebase handler derives: a token is Member only while its sign-in is verified
        var stillVerified = await _signIns.IsVerifiedAsync(UserId, ThisDevice);
        var authState = stillVerified ? AuthState.Member : AuthState.VerificationRequired;
        await using var after = await StartAsync(authState);

        var response = await after.Client.PostAsync(RequestCodePath, content: null);

        stillVerified.ShouldBeFalse();
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Private Methods

    // Asserts the member is not deleted and still has both verified sign-ins
    private async Task AssertNotDeletedAsync()
    {
        var stored = await _users.GetByIdAsync(UserId);
        stored.ShouldNotBeNull();
        stored.DeletedAt.ShouldBeNull();
        stored.VerifiedSignIns.ShouldBe(new List<long> { ThisDevice, OtherDevice });
        _accounts.RevokedSessions.ShouldBeEmpty();
    }

    // Builds the endpoints on a test server with the real service and in-memory fakes
    private async Task<TestApp> StartAsync(AuthState authState)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<ICurrentUserContext>(new CurrentUserContext
        {
            IsAuthenticated = authState != AuthState.Anonymous,
            UserId = authState == AuthState.Anonymous ? null : UserId,
            AuthState = authState,
            AuthTime = ThisDevice
        });
        builder.Services.AddSingleton<IAccountDeletionService>(_service);

        var app = builder.Build();
        AccountDeletionEndpoints.Map(app);
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
