// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/PasswordServiceTests.cs
// Version : 1.0.0
// Latest commit: feat/password-change-service
// Author : Gerrah
// Purpose : Proves password change: the rules are checked before a code is sent or used, only the
// right code changes the password, a change signs the member out everywhere and sends a notice,
// and a failed notice or a failed change behaves as it should.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Tests.Fakes;
using TeamSamsara.Shared.Alert;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Tests;

public class PasswordServiceTests : IAsyncLifetime
{
    #region Fields

    private const string UserId = "user-1";
    private const string Email = "jane.doe@example.com";
    private const string NewPassword = "a-good-new-password";
    private const string WrongCode = "not-the-code";

    private readonly InMemoryUserStore _users = new();
    private readonly InMemoryVerificationCodeStore _codes = new();
    private readonly RecordingAlertSender _alerts = new();
    private readonly FakeAccountGateway _accounts = new();
    private readonly FakeClock _clock = new();
    private readonly VerificationSettings _verificationSettings = new();
    private readonly PasswordSettings _passwordSettings = new();
    private readonly PasswordService _service;

    #endregion

    #region Constructors

    public PasswordServiceTests()
    {
        var verification = new VerificationCodeService(
            _codes, _alerts, _clock, Options.Create(_verificationSettings));

        _service = new PasswordService(
            _users,
            _accounts,
            verification,
            _alerts,
            Options.Create(_passwordSettings),
            NullLogger<PasswordService>.Instance);

        _accounts.AddAccount(UserId, Email);
    }

    #endregion

    #region Public Methods

    public async Task InitializeAsync()
    {
        await _users.CreateAsync(new User
        {
            Id = UserId,
            AccessLevel = AccessLevel.Member,
            CreatedAt = _clock.UtcNow
        });
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task RequestCode_SendsAStepUpCodeToTheAccountEmail()
    {
        var result = await _service.RequestChangeCodeAsync(UserId, NewPassword);

        result.Status.ShouldBe(PasswordStatus.Success);
        result.CodeLength.ShouldBe(_verificationSettings.StepUpCodeLength);

        var alert = _alerts.Sent.ShouldHaveSingleItem();
        alert.To.ShouldBe(Email);
        alert.Type.ShouldBe(AlertType.StepUpCode);
    }

    [Fact]
    public async Task RequestCode_WithATooShortPassword_IsInvalid_AndSendsNothing()
    {
        var tooShort = new string('a', _passwordSettings.MinLength - 1);

        var result = await _service.RequestChangeCodeAsync(UserId, tooShort);

        result.Status.ShouldBe(PasswordStatus.InvalidPassword);
        _alerts.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task RequestCode_WithATooLongPassword_IsInvalid_AndSendsNothing()
    {
        var tooLong = new string('a', _passwordSettings.MaxLength + 1);

        var result = await _service.RequestChangeCodeAsync(UserId, tooLong);

        result.Status.ShouldBe(PasswordStatus.InvalidPassword);
        _alerts.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task RequestCode_AgainTooSoon_ReportsCooldown_AndSendsNothingMore()
    {
        await _service.RequestChangeCodeAsync(UserId, NewPassword);

        var result = await _service.RequestChangeCodeAsync(UserId, NewPassword);

        result.Status.ShouldBe(PasswordStatus.CooldownActive);
        result.RetryAfterSeconds.ShouldBeGreaterThan(0);
        _alerts.Sent.Count.ShouldBe(1);
    }

    [Fact]
    public async Task RequestCode_ForAnUnknownAccount_IsNotFound()
    {
        var result = await _service.RequestChangeCodeAsync("ghost", NewPassword);

        result.Status.ShouldBe(PasswordStatus.AccountNotFound);
        _alerts.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task RequestCode_ForAGuest_IsNotFound()
    {
        await _users.CreateAsync(new User
        {
            Id = "guest-1",
            AccessLevel = AccessLevel.Guest,
            CreatedAt = _clock.UtcNow
        });
        _accounts.AddAccount("guest-1", "guest@example.com");

        var result = await _service.RequestChangeCodeAsync("guest-1", NewPassword);

        result.Status.ShouldBe(PasswordStatus.AccountNotFound);
        _alerts.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task Change_WithTheRightCode_SetsThePassword_SignsOutEverywhere_AndSendsANotice()
    {
        var code = await RequestCodeAsync();

        var status = await _service.ChangeAsync(UserId, NewPassword, code);

        status.ShouldBe(PasswordStatus.Success);
        _accounts.Passwords[UserId].ShouldBe(NewPassword);
        _accounts.RevokedSessions.ShouldHaveSingleItem().ShouldBe(UserId);

        var notice = _alerts.Sent.Last();
        notice.Type.ShouldBe(AlertType.PasswordChanged);
        notice.To.ShouldBe(Email);
    }

    [Fact]
    public async Task Change_WithAWrongCode_ChangesNothing()
    {
        await RequestCodeAsync();

        var status = await _service.ChangeAsync(UserId, NewPassword, WrongCode);

        status.ShouldBe(PasswordStatus.InvalidCode);
        _accounts.Passwords.ShouldBeEmpty();
        _accounts.RevokedSessions.ShouldBeEmpty();
        _alerts.Sent.ShouldNotContain(alert => alert.Type == AlertType.PasswordChanged);
    }

    [Fact]
    public async Task Change_WithAnUnacceptablePassword_IsInvalid_AndDoesNotUseUpTheCode()
    {
        var code = await RequestCodeAsync();
        var tooShort = new string('a', _passwordSettings.MinLength - 1);

        var rejected = await _service.ChangeAsync(UserId, tooShort, code);
        var accepted = await _service.ChangeAsync(UserId, NewPassword, code);

        rejected.ShouldBe(PasswordStatus.InvalidPassword);
        accepted.ShouldBe(PasswordStatus.Success);
        _accounts.Passwords[UserId].ShouldBe(NewPassword);
    }

    [Fact]
    public async Task Change_CodeCannotBeUsedTwice()
    {
        var code = await RequestCodeAsync();
        await _service.ChangeAsync(UserId, NewPassword, code);

        var second = await _service.ChangeAsync(UserId, "another-new-password", code);

        second.ShouldBe(PasswordStatus.NoPendingCode);
        _accounts.Passwords[UserId].ShouldBe(NewPassword);
    }

    [Fact]
    public async Task Change_WithNoPendingCode_IsNoPendingCode()
    {
        var status = await _service.ChangeAsync(UserId, NewPassword, "12345678");

        status.ShouldBe(PasswordStatus.NoPendingCode);
        _accounts.Passwords.ShouldBeEmpty();
    }

    [Fact]
    public async Task Change_WithTooManyWrongGuesses_BurnsTheCode_EvenForTheRealOne()
    {
        var code = await RequestCodeAsync();

        for (var attempt = 1; attempt < _verificationSettings.MaxFailedAttempts; attempt++)
        {
            var wrong = await _service.ChangeAsync(UserId, NewPassword, WrongCode);
            wrong.ShouldBe(PasswordStatus.InvalidCode);
        }

        var final = await _service.ChangeAsync(UserId, NewPassword, WrongCode);
        var real = await _service.ChangeAsync(UserId, NewPassword, code);

        final.ShouldBe(PasswordStatus.TooManyAttempts);
        real.ShouldBe(PasswordStatus.NoPendingCode);
        _accounts.Passwords.ShouldBeEmpty();
    }

    [Fact]
    public async Task Change_WithAnExpiredCode_IsCodeExpired()
    {
        var code = await RequestCodeAsync();
        _clock.Advance(TimeSpan.FromMinutes(_verificationSettings.ExpiryMinutes));

        var status = await _service.ChangeAsync(UserId, NewPassword, code);

        status.ShouldBe(PasswordStatus.CodeExpired);
        _accounts.Passwords.ShouldBeEmpty();
    }

    [Fact]
    public async Task Change_ForAnUnknownAccount_IsNotFound()
    {
        var status = await _service.ChangeAsync("ghost", NewPassword, "12345678");

        status.ShouldBe(PasswordStatus.AccountNotFound);
    }

    [Fact]
    public async Task Change_WhenTheNoticeEmailFails_StillSucceeds()
    {
        var code = await RequestCodeAsync();
        _alerts.FailingTypes.Add(AlertType.PasswordChanged);

        var status = await _service.ChangeAsync(UserId, NewPassword, code);

        status.ShouldBe(PasswordStatus.Success);
        _accounts.Passwords[UserId].ShouldBe(NewPassword);
        _accounts.RevokedSessions.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Change_WhenSettingThePasswordFails_Throws_AndSignsNobodyOut()
    {
        var code = await RequestCodeAsync();
        _accounts.SetPasswordShouldFail = true;

        await Should.ThrowAsync<InvalidOperationException>(
            () => _service.ChangeAsync(UserId, NewPassword, code));

        _accounts.RevokedSessions.ShouldBeEmpty();
        _alerts.Sent.ShouldNotContain(alert => alert.Type == AlertType.PasswordChanged);
    }

    #endregion

    #region Private Methods

    // Requests a change code and returns the code the member would have received
    private async Task<string> RequestCodeAsync()
    {
        await _service.RequestChangeCodeAsync(UserId, NewPassword);

        return _alerts.LastCode;
    }

    #endregion
}
