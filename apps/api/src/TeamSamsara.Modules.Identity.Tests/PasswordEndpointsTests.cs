// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/PasswordEndpointsTests.cs
// Version : 1.1.0
// Latest commit: fix/password-change-verified-sign-ins
// Author : Gerrah
// Purpose : Proves the password endpoints refuse non-members before reading the request body,
// reject missing or malformed bodies from members, and answer each step of a password change
// with the right HTTP status.

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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

public class PasswordEndpointsTests
{
    #region Fields

    private const string UserId = "user-1";
    private const string Email = "jane.doe@example.com";
    private const string NewPassword = "a-good-new-password";
    private const string WrongCode = "not-the-code";
    private const string RequestCodePath = "/password/request-code";
    private const string ChangePath = "/password/change";

    private readonly InMemoryUserStore _users = new();
    private readonly InMemoryVerificationCodeStore _codes = new();
    private readonly InMemoryCacheService _cache = new();
    private readonly RecordingAlertSender _alerts = new();
    private readonly FakeAccountGateway _accounts = new();
    private readonly FakeClock _clock = new();
    private readonly VerificationSettings _verificationSettings = new();
    private readonly PasswordSettings _passwordSettings = new();

    #endregion

    #region Public Methods

    [Theory]
    [InlineData(RequestCodePath)]
    [InlineData(ChangePath)]
    public async Task AnAnonymousCaller_GetsUnauthorized_EvenWithoutABody(string path)
    {
        await using var app = await StartAsync(AuthState.Anonymous);

        var response = await app.Client.PostAsync(path, content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(RequestCodePath, AuthState.RegistrationIncomplete)]
    [InlineData(RequestCodePath, AuthState.VerificationRequired)]
    [InlineData(ChangePath, AuthState.RegistrationIncomplete)]
    [InlineData(ChangePath, AuthState.VerificationRequired)]
    public async Task AnUnverifiedCaller_GetsForbidden_EvenWithoutABody(string path, AuthState authState)
    {
        await using var app = await StartAsync(authState);

        var response = await app.Client.PostAsync(path, content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        _alerts.Sent.ShouldBeEmpty();
        _accounts.Passwords.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(RequestCodePath)]
    [InlineData(ChangePath)]
    public async Task AMember_SendingNoBody_GetsBadRequest(string path)
    {
        await using var app = await StartAsync(AuthState.Member);

        var response = await app.Client.PostAsync(path, content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _alerts.Sent.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(RequestCodePath)]
    [InlineData(ChangePath)]
    public async Task AMember_SendingANonJsonBody_GetsBadRequest(string path)
    {
        await using var app = await StartAsync(AuthState.Member);
        var body = new StringContent("hello", Encoding.UTF8, "text/plain");

        var response = await app.Client.PostAsync(path, body);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _alerts.Sent.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(RequestCodePath)]
    [InlineData(ChangePath)]
    public async Task AMember_SendingMalformedJson_GetsBadRequest(string path)
    {
        await using var app = await StartAsync(AuthState.Member);
        var body = new StringContent("{ not json", Encoding.UTF8, "application/json");

        var response = await app.Client.PostAsync(path, body);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _alerts.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task RequestCode_WithAnEmptyPassword_GetsBadRequest_AndSendsNothing()
    {
        await using var app = await StartAsync(AuthState.Member);

        var response = await app.Client.PostAsync(
            RequestCodePath, JsonContent.Create(new PasswordCodeRequest(string.Empty)));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _alerts.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task RequestCode_ForAMember_SendsTheCode_AndReportsItsLength()
    {
        await using var app = await StartAsync(AuthState.Member);

        var response = await app.Client.PostAsync(
            RequestCodePath, JsonContent.Create(new PasswordCodeRequest(NewPassword)));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var alert = _alerts.Sent.ShouldHaveSingleItem();
        alert.To.ShouldBe(Email);
        alert.Type.ShouldBe(AlertType.StepUpCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("codeLength").GetInt32().ShouldBe(_verificationSettings.StepUpCodeLength);
    }

    [Fact]
    public async Task RequestCode_WithATooShortPassword_GetsBadRequest_AndSendsNothing()
    {
        await using var app = await StartAsync(AuthState.Member);
        var tooShort = new string('a', _passwordSettings.MinLength - 1);

        var response = await app.Client.PostAsync(
            RequestCodePath, JsonContent.Create(new PasswordCodeRequest(tooShort)));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _alerts.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task RequestCode_AgainTooSoon_GetsTooManyRequests_AndSendsNothingMore()
    {
        await using var app = await StartAsync(AuthState.Member);
        await RequestCodeAsync(app);

        var response = await app.Client.PostAsync(
            RequestCodePath, JsonContent.Create(new PasswordCodeRequest(NewPassword)));

        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        _alerts.Sent.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Change_WithAnEmptyCode_GetsBadRequest()
    {
        await using var app = await StartAsync(AuthState.Member);

        var response = await ChangeAsync(app, NewPassword, string.Empty);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _accounts.Passwords.ShouldBeEmpty();
    }

    [Fact]
    public async Task Change_WithTheRightCode_SetsThePassword_SignsOutEverywhere_AndSendsANotice()
    {
        await using var app = await StartAsync(AuthState.Member);
        var code = await RequestCodeAsync(app);

        var response = await ChangeAsync(app, NewPassword, code);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _accounts.Passwords[UserId].ShouldBe(NewPassword);
        _accounts.RevokedSessions.ShouldHaveSingleItem().ShouldBe(UserId);
        _alerts.Sent.Last().Type.ShouldBe(AlertType.PasswordChanged);
    }

    [Fact]
    public async Task Change_WithAWrongCode_GetsBadRequest_AndChangesNothing()
    {
        await using var app = await StartAsync(AuthState.Member);
        await RequestCodeAsync(app);

        var response = await ChangeAsync(app, NewPassword, WrongCode);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _accounts.Passwords.ShouldBeEmpty();
        _accounts.RevokedSessions.ShouldBeEmpty();
    }

    [Fact]
    public async Task Change_WithAnUnacceptablePassword_GetsBadRequest_AndDoesNotUseUpTheCode()
    {
        await using var app = await StartAsync(AuthState.Member);
        var code = await RequestCodeAsync(app);
        var tooShort = new string('a', _passwordSettings.MinLength - 1);

        var rejected = await ChangeAsync(app, tooShort, code);
        var accepted = await ChangeAsync(app, NewPassword, code);

        rejected.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        accepted.StatusCode.ShouldBe(HttpStatusCode.OK);
        _accounts.Passwords[UserId].ShouldBe(NewPassword);
    }

    [Fact]
    public async Task Change_WithNoPendingCode_GetsBadRequest()
    {
        await using var app = await StartAsync(AuthState.Member);

        var response = await ChangeAsync(app, NewPassword, "12345678");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _accounts.Passwords.ShouldBeEmpty();
    }

    [Fact]
    public async Task Change_WithAnExpiredCode_GetsBadRequest()
    {
        await using var app = await StartAsync(AuthState.Member);
        var code = await RequestCodeAsync(app);
        _clock.Advance(TimeSpan.FromMinutes(_verificationSettings.ExpiryMinutes));

        var response = await ChangeAsync(app, NewPassword, code);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _accounts.Passwords.ShouldBeEmpty();
    }

    [Fact]
    public async Task Change_WithTooManyWrongGuesses_GetsTooManyRequests()
    {
        await using var app = await StartAsync(AuthState.Member);
        await RequestCodeAsync(app);

        for (var attempt = 1; attempt < _verificationSettings.MaxFailedAttempts; attempt++)
        {
            var wrong = await ChangeAsync(app, NewPassword, WrongCode);
            wrong.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        var final = await ChangeAsync(app, NewPassword, WrongCode);

        final.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        _accounts.Passwords.ShouldBeEmpty();
    }

    #endregion

    #region Private Methods

    // Requests a change code for the standard new password and returns the code the member
    // would have received by email
    private async Task<string> RequestCodeAsync(TestApp app)
    {
        await app.Client.PostAsync(
            RequestCodePath, JsonContent.Create(new PasswordCodeRequest(NewPassword)));

        return _alerts.LastCode;
    }

    private static Task<HttpResponseMessage> ChangeAsync(TestApp app, string newPassword, string code)
    {
        return app.Client.PostAsync(
            ChangePath, JsonContent.Create(new ChangePasswordRequest(newPassword, code)));
    }

    private async Task<TestApp> StartAsync(AuthState authState)
    {
        await _users.CreateAsync(new User
        {
            Id = UserId,
            AccessLevel = AccessLevel.Member,
            CreatedAt = _clock.UtcNow
        });
        _accounts.AddAccount(UserId, Email);

        var verification = new VerificationCodeService(
            _codes, _alerts, _clock, Options.Create(_verificationSettings));

        var signIns = new SignInTracker(_users, _cache, Options.Create(new AuthenticationSettings()));

        var passwords = new PasswordService(
            _users,
            _accounts,
            signIns,
            verification,
            _alerts,
            Options.Create(_passwordSettings),
            NullLogger<PasswordService>.Instance);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<ICurrentUserContext>(new CurrentUserContext
        {
            IsAuthenticated = authState != AuthState.Anonymous,
            UserId = authState == AuthState.Anonymous ? null : UserId,
            AuthState = authState
        });
        builder.Services.AddSingleton<IPasswordService>(passwords);

        var app = builder.Build();
        PasswordEndpoints.Map(app);
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
