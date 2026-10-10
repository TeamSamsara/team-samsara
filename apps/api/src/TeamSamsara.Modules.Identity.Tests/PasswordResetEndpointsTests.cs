// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/PasswordResetEndpointsTests.cs
// Version : 1.0.0
// Latest commit: feat/password-reset-endpoints
// Author : Gerrah
// Purpose : Proves the password reset endpoints work for signed-out callers, reject missing or
// malformed bodies, map each outcome to the right HTTP status, and limit requests per IP.

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TeamSamsara.Modules.Identity.Endpoints;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Shared.Http;

namespace TeamSamsara.Modules.Identity.Tests;

public class PasswordResetEndpointsTests
{
    #region Fields

    private const string Email = "jane.doe@example.com";
    private const string Code = "12345678";
    private const string ResetToken = "reset-token";
    private const string NewPassword = "a-good-new-password";
    private const string RequestPath = IdentityRoutes.PasswordResetGroup + IdentityRoutes.PasswordResetRequest;
    private const string VerifyPath = IdentityRoutes.PasswordResetGroup + IdentityRoutes.PasswordResetVerify;
    private const string ConfirmPath = IdentityRoutes.PasswordResetGroup + IdentityRoutes.PasswordResetConfirm;
    private const string ClientIpHeader = "X-Test-Ip";
    private const int PermitLimit = 3;

    private readonly StubPasswordResetService _resets = new();

    #endregion

    #region Public Methods

    [Theory]
    [InlineData(RequestPath)]
    [InlineData(VerifyPath)]
    [InlineData(ConfirmPath)]
    public async Task ASignedOutCaller_SendingNoBody_GetsBadRequest(string path)
    {
        await using var app = await StartAsync();

        var response = await app.Client.PostAsync(path, content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _resets.Calls.ShouldBe(0);
    }

    [Theory]
    [InlineData(RequestPath)]
    [InlineData(VerifyPath)]
    [InlineData(ConfirmPath)]
    public async Task ASignedOutCaller_SendingANonJsonBody_GetsBadRequest(string path)
    {
        await using var app = await StartAsync();
        var body = new StringContent("hello", Encoding.UTF8, "text/plain");

        var response = await app.Client.PostAsync(path, body);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _resets.Calls.ShouldBe(0);
    }

    [Theory]
    [InlineData(RequestPath)]
    [InlineData(VerifyPath)]
    [InlineData(ConfirmPath)]
    public async Task ASignedOutCaller_SendingMalformedJson_GetsBadRequest(string path)
    {
        await using var app = await StartAsync();
        var body = new StringContent("{ not json", Encoding.UTF8, "application/json");

        var response = await app.Client.PostAsync(path, body);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _resets.Calls.ShouldBe(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Request_WithABlankEmail_GetsBadRequest(string email)
    {
        await using var app = await StartAsync();

        var response = await app.Client.PostAsync(
            RequestPath, JsonContent.Create(new PasswordResetRequest(email)));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _resets.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task Request_WithAnOversizedEmail_GetsBadRequest()
    {
        await using var app = await StartAsync();
        var email = new string('a', 250) + "@example.com";

        var response = await app.Client.PostAsync(
            RequestPath, JsonContent.Create(new PasswordResetRequest(email)));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _resets.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task Request_WithAnEmail_AnswersOk_WithTheCodeDetails()
    {
        await using var app = await StartAsync();
        _resets.RequestResult = new PasswordCodeResult(PasswordStatus.Success, 8, 60);

        var response = await app.Client.PostAsync(
            RequestPath, JsonContent.Create(new PasswordResetRequest(Email)));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("codeLength").GetInt32().ShouldBe(8);
        json.RootElement.GetProperty("retryAfterSeconds").GetInt32().ShouldBe(60);
        _resets.RequestedEmails.ShouldHaveSingleItem().ShouldBe(Email);
    }

    [Theory]
    [InlineData("", Code)]
    [InlineData(Email, "")]
    [InlineData(Email, "   ")]
    public async Task Verify_WithABlankEmailOrCode_GetsBadRequest(string email, string code)
    {
        await using var app = await StartAsync();

        var response = await app.Client.PostAsync(
            VerifyPath, JsonContent.Create(new VerifyResetCodeRequest(email, code)));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _resets.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task Verify_WithAValidCode_AnswersOk_WithTheResetToken()
    {
        await using var app = await StartAsync();
        _resets.VerifyResult = new PasswordResetTokenResult(PasswordStatus.Success, ResetToken);

        var response = await app.Client.PostAsync(
            VerifyPath, JsonContent.Create(new VerifyResetCodeRequest(Email, Code)));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("resetToken").GetString().ShouldBe(ResetToken);
    }

    [Theory]
    [InlineData(PasswordStatus.InvalidCode, HttpStatusCode.BadRequest)]
    [InlineData(PasswordStatus.CodeExpired, HttpStatusCode.BadRequest)]
    [InlineData(PasswordStatus.TooManyAttempts, HttpStatusCode.TooManyRequests)]
    [InlineData(PasswordStatus.AccountNotFound, HttpStatusCode.NotFound)]
    public async Task Verify_WithAFailedCheck_AnswersTheMatchingStatus(
        PasswordStatus status,
        HttpStatusCode expected)
    {
        await using var app = await StartAsync();
        _resets.VerifyResult = new PasswordResetTokenResult(status, null);

        var response = await app.Client.PostAsync(
            VerifyPath, JsonContent.Create(new VerifyResetCodeRequest(Email, Code)));

        response.StatusCode.ShouldBe(expected);
    }

    [Theory]
    [InlineData("", NewPassword)]
    [InlineData("   ", NewPassword)]
    [InlineData(ResetToken, "")]
    public async Task Confirm_WithABlankTokenOrPassword_GetsBadRequest(string token, string password)
    {
        await using var app = await StartAsync();

        var response = await app.Client.PostAsync(
            ConfirmPath, JsonContent.Create(new SetNewPasswordRequest(token, password)));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _resets.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task Confirm_WithAValidToken_AnswersOk_AndPassesTheValuesOn()
    {
        await using var app = await StartAsync();
        _resets.ConfirmStatus = PasswordStatus.Success;

        var response = await app.Client.PostAsync(
            ConfirmPath, JsonContent.Create(new SetNewPasswordRequest(ResetToken, NewPassword)));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _resets.ConfirmedWith.ShouldBe((ResetToken, NewPassword));
    }

    [Theory]
    [InlineData(PasswordStatus.InvalidPassword, HttpStatusCode.BadRequest)]
    [InlineData(PasswordStatus.InvalidResetToken, HttpStatusCode.BadRequest)]
    [InlineData(PasswordStatus.AccountNotFound, HttpStatusCode.NotFound)]
    public async Task Confirm_WithAFailure_AnswersTheMatchingStatus(
        PasswordStatus status,
        HttpStatusCode expected)
    {
        await using var app = await StartAsync();
        _resets.ConfirmStatus = status;

        var response = await app.Client.PostAsync(
            ConfirmPath, JsonContent.Create(new SetNewPasswordRequest(ResetToken, NewPassword)));

        response.StatusCode.ShouldBe(expected);
    }

    [Fact]
    public async Task AnIp_GetsTooManyRequests_OnceItSpentItsLimit_AcrossAllResetEndpoints()
    {
        await using var app = await StartAsync();

        for (var attempt = 0; attempt < PermitLimit; attempt++)
        {
            var allowed = await PostFromAsync(app, RequestPath, "203.0.113.10");
            allowed.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        var limited = await PostFromAsync(app, ConfirmPath, "203.0.113.10");

        limited.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        limited.Headers.Contains("Retry-After").ShouldBeTrue();
    }

    [Fact]
    public async Task TheLimit_IsPerIp_SoAnotherIpIsStillServed()
    {
        await using var app = await StartAsync();

        for (var attempt = 0; attempt <= PermitLimit; attempt++)
        {
            await PostFromAsync(app, RequestPath, "203.0.113.10");
        }

        var other = await PostFromAsync(app, RequestPath, "203.0.113.11");

        other.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region Private Methods

    // Posts a valid request body for the path, pretending to come from the given IP
    private static async Task<HttpResponseMessage> PostFromAsync(TestApp app, string path, string ip)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = path == RequestPath
                ? JsonContent.Create(new PasswordResetRequest(Email))
                : JsonContent.Create(new SetNewPasswordRequest(ResetToken, NewPassword))
        };
        message.Headers.Add(ClientIpHeader, ip);

        return await app.Client.SendAsync(message);
    }

    private async Task<TestApp> StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IPasswordResetService>(_resets);
        builder.Services.AddSamsaraRateLimiting(new RateLimitSettings { PermitLimit = PermitLimit });

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
        PasswordResetEndpoints.Map(app);
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

    // Answers with configured results and records what the endpoints asked for
    private sealed class StubPasswordResetService : IPasswordResetService
    {
        public PasswordCodeResult RequestResult { get; set; } =
            new(PasswordStatus.Success, 8, 60);

        public PasswordResetTokenResult VerifyResult { get; set; } =
            new(PasswordStatus.InvalidCode, null);

        public PasswordStatus ConfirmStatus { get; set; } = PasswordStatus.Success;

        public int Calls { get; private set; }

        public List<string> RequestedEmails { get; } = new();

        public (string Token, string Password)? ConfirmedWith { get; private set; }

        public Task<PasswordCodeResult> RequestResetAsync(
            string email,
            CancellationToken cancellationToken)
        {
            Calls++;
            RequestedEmails.Add(email);

            return Task.FromResult(RequestResult);
        }

        public Task<PasswordResetTokenResult> VerifyCodeAsync(
            string email,
            string code,
            CancellationToken cancellationToken)
        {
            Calls++;

            return Task.FromResult(VerifyResult);
        }

        public Task<PasswordStatus> SetNewPasswordAsync(
            string resetToken,
            string newPassword,
            CancellationToken cancellationToken)
        {
            Calls++;
            ConfirmedWith = (resetToken, newPassword);

            return Task.FromResult(ConfirmStatus);
        }
    }

    #endregion
}
