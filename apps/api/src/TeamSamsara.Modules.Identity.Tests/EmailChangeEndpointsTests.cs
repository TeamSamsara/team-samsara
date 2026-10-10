// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/EmailChangeEndpointsTests.cs
// Version : 1.0.0
// Latest commit: feat/email-change-endpoints
// Author : Gerrah
// Purpose : Proves the email change endpoints refuse non-members, reject bad bodies, and answer each step with the right HTTP status.

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

public class EmailChangeEndpointsTests
{
    #region Fields

    private const string UserId = "user-1";
    private const string OtherUserId = "user-2";
    private const string Email = "jane.doe@example.com";
    private const string NewEmail = "jane.new@example.com";
    private const string TakenEmail = "taken@example.com";
    private const string WrongCode = "not-the-code";
    private const string RequestPath = "/email/request";
    private const string ConfirmPath = "/email/confirm";

    private readonly InMemoryUserStore _users = new();
    private readonly InMemoryVerificationCodeStore _codes = new();
    private readonly InMemoryEmailChangeRequestStore _requests = new();
    private readonly InMemoryCacheService _cache = new();
    private readonly RecordingAlertSender _alerts = new();
    private readonly RecordingBackgroundTaskQueue _queue = new();
    private readonly FakeAccountGateway _accounts = new();
    private readonly FakeClock _clock = new();
    private readonly VerificationSettings _settings = new();

    #endregion

    #region Public Methods

    [Theory]
    [InlineData(RequestPath)]
    [InlineData(ConfirmPath)]
    public async Task AnAnonymousCaller_GetsUnauthorized_EvenWithoutABody(string path)
    {
        await using var app = await StartAsync(AuthState.Anonymous);

        var response = await app.Client.PostAsync(path, content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(RequestPath, AuthState.RegistrationIncomplete)]
    [InlineData(RequestPath, AuthState.VerificationRequired)]
    [InlineData(ConfirmPath, AuthState.RegistrationIncomplete)]
    [InlineData(ConfirmPath, AuthState.VerificationRequired)]
    public async Task AnUnverifiedCaller_GetsForbidden_AndNothingIsSaved(string path, AuthState authState)
    {
        await using var app = await StartAsync(authState);

        var response = await app.Client.PostAsync(path, content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        _requests.Count.ShouldBe(0);
        _queue.Count.ShouldBe(0);
    }

    [Theory]
    [InlineData(RequestPath)]
    [InlineData(ConfirmPath)]
    public async Task AMember_SendingNoBody_GetsBadRequest(string path)
    {
        await using var app = await StartAsync(AuthState.Member);

        var response = await app.Client.PostAsync(path, content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _requests.Count.ShouldBe(0);
    }

    [Theory]
    [InlineData(RequestPath)]
    [InlineData(ConfirmPath)]
    public async Task AMember_SendingANonJsonBody_GetsBadRequest(string path)
    {
        await using var app = await StartAsync(AuthState.Member);
        var body = new StringContent("hello", Encoding.UTF8, "text/plain");

        var response = await app.Client.PostAsync(path, body);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _requests.Count.ShouldBe(0);
    }

    [Theory]
    [InlineData(RequestPath)]
    [InlineData(ConfirmPath)]
    public async Task AMember_SendingMalformedJson_GetsBadRequest(string path)
    {
        await using var app = await StartAsync(AuthState.Member);
        var body = new StringContent("{ not json", Encoding.UTF8, "application/json");

        var response = await app.Client.PostAsync(path, body);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _requests.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Request_ForAMember_GetsOk_AndReportsTheCodeLengthAndWait()
    {
        await using var app = await StartAsync(AuthState.Member);

        var response = await RequestAsync(app, NewEmail);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("codeLength").GetInt32().ShouldBe(_settings.EmailChangeCodeLength);
        body.RootElement.GetProperty("retryAfterSeconds").GetInt32().ShouldBe(_settings.ResendCooldownSeconds);
    }

    [Fact]
    public async Task Request_SendsTheCodesOnlyWhenTheQueuedWorkRuns()
    {
        await using var app = await StartAsync(AuthState.Member);

        await RequestAsync(app, NewEmail);
        _alerts.Sent.ShouldBeEmpty();

        await _queue.RunAllAsync(app.Services);

        _alerts.Sent.Select(alert => alert.To).ShouldBe(new[] { Email, NewEmail }, ignoreOrder: true);
    }

    [Fact]
    public async Task Request_ForAFreeAddressAndATakenAddress_GivesTheSameResponse()
    {
        _accounts.AddAccount(OtherUserId, TakenEmail);
        await using var app = await StartAsync(AuthState.Member);

        var free = await RequestAsync(app, NewEmail);
        _clock.Advance(TimeSpan.FromSeconds(_settings.ResendCooldownSeconds + 1));
        var taken = await RequestAsync(app, TakenEmail);

        taken.StatusCode.ShouldBe(free.StatusCode);
        (await taken.Content.ReadAsStringAsync()).ShouldBe(await free.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Request_WithAnInvalidAddress_GetsBadRequest_AndQueuesNothing()
    {
        await using var app = await StartAsync(AuthState.Member);

        var response = await RequestAsync(app, "not-an-email");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _queue.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Request_AgainTooSoon_GetsTooManyRequests()
    {
        await using var app = await StartAsync(AuthState.Member);
        await RequestAsync(app, NewEmail);

        var response = await RequestAsync(app, NewEmail);

        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Confirm_WithBothRightCodes_ChangesTheEmail_AndSignsOutEverywhere()
    {
        await using var app = await StartAsync(AuthState.Member);
        var codes = await RequestAndGetCodesAsync(app);

        var response = await ConfirmAsync(app, codes.Old, codes.New);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await _accounts.GetEmailAsync(UserId)).ShouldBe(NewEmail);
        _accounts.RevokedSessions.ShouldHaveSingleItem().ShouldBe(UserId);
        _alerts.Sent.Last().Type.ShouldBe(AlertType.EmailChanged);
    }

    [Fact]
    public async Task Confirm_WithAWrongCode_GetsBadRequest_AndChangesNothing()
    {
        await using var app = await StartAsync(AuthState.Member);
        var codes = await RequestAndGetCodesAsync(app);

        var response = await ConfirmAsync(app, codes.Old, WrongCode);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await _accounts.GetEmailAsync(UserId)).ShouldBe(Email);
        _accounts.RevokedSessions.ShouldBeEmpty();
    }

    [Fact]
    public async Task Confirm_ForATakenAddress_GivesTheSameResponseAsAWrongCode()
    {
        _accounts.AddAccount(OtherUserId, TakenEmail);
        await using var app = await StartAsync(AuthState.Member);
        await RequestAsync(app, TakenEmail);
        await _queue.RunAllAsync(app.Services);

        var response = await ConfirmAsync(app, CodeSentTo(Email), WrongCode);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await _accounts.GetEmailAsync(UserId)).ShouldBe(Email);
    }

    [Fact]
    public async Task Confirm_WithNoPendingRequest_GetsBadRequest()
    {
        await using var app = await StartAsync(AuthState.Member);

        var response = await ConfirmAsync(app, "12345678", "12345678");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Confirm_WithTooManyWrongGuesses_GetsTooManyRequests()
    {
        await using var app = await StartAsync(AuthState.Member);
        var codes = await RequestAndGetCodesAsync(app);

        for (var attempt = 1; attempt < _settings.MaxFailedAttempts; attempt++)
        {
            var wrong = await ConfirmAsync(app, codes.Old, WrongCode);
            wrong.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        var final = await ConfirmAsync(app, codes.Old, WrongCode);

        final.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        (await _accounts.GetEmailAsync(UserId)).ShouldBe(Email);
    }

    #endregion

    #region Private Methods

    // Posts an email change request for the given address.
    private static Task<HttpResponseMessage> RequestAsync(TestApp app, string newEmail)
    {
        return app.Client.PostAsync(RequestPath, JsonContent.Create(new ChangeEmailRequest(newEmail)));
    }

    // Posts the two codes to confirm the change.
    private static Task<HttpResponseMessage> ConfirmAsync(TestApp app, string oldCode, string newCode)
    {
        return app.Client.PostAsync(
            ConfirmPath, JsonContent.Create(new ConfirmEmailChangeRequest(oldCode, newCode)));
    }

    // The code most recently emailed to an address.
    private string CodeSentTo(string email)
    {
        return _alerts.Sent
            .Last(alert => alert.To == email && alert.TemplateData.ContainsKey(AlertTemplateKeys.Code))
            .TemplateData[AlertTemplateKeys.Code];
    }

    // Requests a change, runs the queued delivery and returns the two codes the member received.
    private async Task<(string Old, string New)> RequestAndGetCodesAsync(TestApp app)
    {
        await RequestAsync(app, NewEmail);
        await _queue.RunAllAsync(app.Services);

        return (CodeSentTo(Email), CodeSentTo(NewEmail));
    }

    // Builds the endpoints on a test server with the real service and in-memory fakes.
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
            _codes, _alerts, _clock, Options.Create(_settings));

        var signIns = new SignInTracker(_users, _cache, Options.Create(new AuthenticationSettings()));

        var delivery = new EmailChangeDelivery(
            _accounts, verification, NullLogger<EmailChangeDelivery>.Instance);

        var service = new EmailChangeService(
            _queue,
            verification,
            _requests,
            _users,
            _accounts,
            signIns,
            _alerts,
            _clock,
            Options.Create(_settings),
            NullLogger<EmailChangeService>.Instance);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<ICurrentUserContext>(new CurrentUserContext
        {
            IsAuthenticated = authState != AuthState.Anonymous,
            UserId = authState == AuthState.Anonymous ? null : UserId,
            AuthState = authState
        });
        builder.Services.AddSingleton<IEmailChangeService>(service);
        builder.Services.AddSingleton<IEmailChangeDelivery>(delivery);

        var app = builder.Build();
        EmailChangeEndpoints.Map(app);
        await app.StartAsync();

        var server = (TestServer)app.Services.GetRequiredService<IServer>();
        return new TestApp(app, server.CreateClient());
    }

    #endregion

    #region Nested Types

    private sealed class TestApp(WebApplication app, HttpClient client) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;

        public IServiceProvider Services => app.Services;

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
        }
    }

    #endregion
}
