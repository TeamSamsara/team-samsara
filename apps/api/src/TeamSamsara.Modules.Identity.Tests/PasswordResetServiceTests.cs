// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/PasswordResetServiceTests.cs
// Version : 1.0.0
// Latest commit: feat/password-reset-service
// Author : Gerrah
// Purpose : Proves password reset: requests answer identically for every address, a confirmed code
// yields a single-use expiring token, and the token sets a new password and ends every session.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Tests.Fakes;
using TeamSamsara.Shared.Alert;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Tests;

public class PasswordResetServiceTests : IAsyncLifetime
{
    #region Fields

    private const string UserId = "user-1";
    private const string Email = "jane.doe@example.com";
    private const string UnknownEmail = "nobody@example.com";
    private const string NewPassword = "a-good-new-password";
    private const string WrongCode = "not-the-code";
    private const string KnownClientFingerprint = "fingerprint-a";
    private const long FirstSignIn = 1_700_000_000;

    private readonly ClientInfo _knownClient = new("203.0.113.7", "TestBrowser", KnownClientFingerprint);
    private readonly ClientInfo _unknownClient = new("198.51.100.9", "OtherBrowser", "fingerprint-z");

    private readonly InMemoryUserStore _users = new();
    private readonly InMemoryVerificationCodeStore _codes = new();
    private readonly InMemoryPasswordResetTokenStore _tokens = new();
    private readonly InMemoryCacheService _cache = new();
    private readonly RecordingAlertSender _alerts = new();
    private readonly RecordingBackgroundTaskQueue _queue = new();
    private readonly FakeAccountGateway _accounts = new();
    private readonly FakeClock _clock = new();
    private readonly FakeGuardDog _guardDog = new();
    private readonly VerificationSettings _verificationSettings = new();
    private readonly PasswordSettings _passwordSettings = new();
    private readonly SignInTracker _signIns;
    private readonly ServiceProvider _provider;
    private readonly PasswordResetService _service;

    #endregion

    #region Constructors

    public PasswordResetServiceTests()
    {
        var verification = new VerificationCodeService(
            _codes, _alerts, _clock, Options.Create(_verificationSettings));

        var delivery = new PasswordResetDelivery(
            _users,
            _accounts,
            verification,
            _guardDog,
            _alerts,
            NullLogger<PasswordResetDelivery>.Instance);

        _signIns = new SignInTracker(_users, _cache, Options.Create(new AuthenticationSettings()));

        _provider = new ServiceCollection()
            .AddSingleton<IPasswordResetDelivery>(delivery)
            .BuildServiceProvider();

        _service = new PasswordResetService(
            _queue,
            _guardDog,
            verification,
            _tokens,
            _users,
            _accounts,
            _signIns,
            _alerts,
            _clock,
            Options.Create(_verificationSettings),
            Options.Create(_passwordSettings),
            NullLogger<PasswordResetService>.Instance);

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
            CreatedAt = _clock.UtcNow,
            VerifiedSignIns = new List<long> { FirstSignIn },
            KnownFingerprints = new List<KnownFingerprint>
            {
                new() { Hash = KnownClientFingerprint, LastSeenAt = _clock.UtcNow }
            }
        });
    }

    public Task DisposeAsync()
    {
        _provider.Dispose();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task Request_ReturnsTheFixedAnswer()
    {
        var result = await _service.RequestResetAsync(Email);

        result.ShouldBe(ExpectedAnswer());
    }

    [Fact]
    public async Task Request_GivesTheSameAnswer_ForAMemberAndAnUnknownAddress()
    {
        var member = await _service.RequestResetAsync(Email);
        var unknown = await _service.RequestResetAsync(UnknownEmail);

        unknown.ShouldBe(member);
    }

    [Fact]
    public async Task Request_SendsNothingUntilTheQueuedWorkRuns()
    {
        await _service.RequestResetAsync(Email);

        _queue.Count.ShouldBe(1);
        _alerts.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task Request_WhenTheQueuedWorkRuns_EmailsTheCodeToAMember()
    {
        await _service.RequestResetAsync(Email);
        await _queue.RunAllAsync(_provider);

        var alert = _alerts.Sent.ShouldHaveSingleItem();
        alert.To.ShouldBe(Email);
        alert.Type.ShouldBe(AlertType.StepUpCode);
    }

    [Fact]
    public async Task Request_ForAnUnknownAddress_SendsNothingWhenTheQueuedWorkRuns()
    {
        await _service.RequestResetAsync(UnknownEmail);
        await _queue.RunAllAsync(_provider);

        _alerts.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task Request_WhenTheQueueIsFull_StillGivesTheSameAnswer()
    {
        _queue.Accepting = false;

        var result = await _service.RequestResetAsync(Email);

        result.ShouldBe(ExpectedAnswer());
        _queue.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Request_WithABlankEmail_QueuesNothing_AndGivesTheSameAnswer()
    {
        var result = await _service.RequestResetAsync("  ");

        result.ShouldBe(ExpectedAnswer());
        _queue.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Request_ReadsTheClientWhenAsked_NotWhenTheQueuedWorkRuns()
    {
        _guardDog.Current = _unknownClient;
        await _service.RequestResetAsync(Email);
        _guardDog.Current = _knownClient;

        await _queue.RunAllAsync(_provider);

        _alerts.Sent.Any(alert => alert.Type == AlertType.PasswordResetRequested).ShouldBeTrue();
    }

    [Fact]
    public async Task Verify_WithTheRightCode_ReturnsAToken()
    {
        var code = await RequestCodeAsync();

        var result = await _service.VerifyCodeAsync(Email, code);

        result.Status.ShouldBe(PasswordStatus.Success);
        result.ResetToken.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Verify_StoresOnlyTheTokenHash_ForTheConfiguredLifetime()
    {
        var code = await RequestCodeAsync();

        var result = await _service.VerifyCodeAsync(Email, code);

        var stored = _tokens.Find(PasswordResetCrypto.HashToken(result.ResetToken!));
        stored.ShouldNotBeNull();
        stored.UserId.ShouldBe(UserId);
        stored.ExpiresAt.ShouldBe(_clock.UtcNow.AddMinutes(_passwordSettings.ResetTokenMinutes));
        _tokens.Find(result.ResetToken!).ShouldBeNull();
    }

    [Fact]
    public async Task Verify_TheSameCodeCannotBeUsedTwice()
    {
        var code = await RequestCodeAsync();

        await _service.VerifyCodeAsync(Email, code);
        var second = await _service.VerifyCodeAsync(Email, code);

        second.Status.ShouldBe(PasswordStatus.InvalidCode);
        second.ResetToken.ShouldBeNull();
        _tokens.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Verify_WithAWrongCode_IsInvalid_AndReturnsNoToken()
    {
        await RequestCodeAsync();

        var result = await _service.VerifyCodeAsync(Email, WrongCode);

        result.Status.ShouldBe(PasswordStatus.InvalidCode);
        result.ResetToken.ShouldBeNull();
        _tokens.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Verify_ForAnUnknownAddress_IsInvalid_LikeAWrongCode()
    {
        await _service.RequestResetAsync(UnknownEmail);
        await _queue.RunAllAsync(_provider);

        var result = await _service.VerifyCodeAsync(UnknownEmail, WrongCode);

        result.Status.ShouldBe(PasswordStatus.InvalidCode);
        result.ResetToken.ShouldBeNull();
    }

    [Fact]
    public async Task Verify_WithNoRequestMade_IsInvalid_NotANoPendingCodeAnswer()
    {
        var result = await _service.VerifyCodeAsync(Email, "12345678");

        result.Status.ShouldBe(PasswordStatus.InvalidCode);
    }

    [Fact]
    public async Task Verify_AnExpiredCode_IsReportedAsExpired()
    {
        var code = await RequestCodeAsync();
        _clock.Advance(TimeSpan.FromMinutes(_verificationSettings.ExpiryMinutes));

        var result = await _service.VerifyCodeAsync(Email, code);

        result.Status.ShouldBe(PasswordStatus.CodeExpired);
    }

    [Fact]
    public async Task Verify_TooManyWrongGuesses_BurnsTheCode()
    {
        await RequestCodeAsync();

        for (var attempt = 1; attempt < _verificationSettings.MaxFailedAttempts; attempt++)
        {
            var result = await _service.VerifyCodeAsync(Email, WrongCode);
            result.Status.ShouldBe(PasswordStatus.InvalidCode);
        }

        var final = await _service.VerifyCodeAsync(Email, WrongCode);

        final.Status.ShouldBe(PasswordStatus.TooManyAttempts);
    }

    [Fact]
    public async Task Verify_IgnoresCaseAndWhitespaceInTheAddress()
    {
        var code = await RequestCodeAsync();

        var result = await _service.VerifyCodeAsync("  JANE.DOE@example.com ", code);

        result.Status.ShouldBe(PasswordStatus.Success);
    }

    [Fact]
    public async Task Verify_WithABlankEmailOrCode_IsInvalid()
    {
        var blankEmail = await _service.VerifyCodeAsync(" ", "12345678");
        var blankCode = await _service.VerifyCodeAsync(Email, " ");

        blankEmail.Status.ShouldBe(PasswordStatus.InvalidCode);
        blankCode.Status.ShouldBe(PasswordStatus.InvalidCode);
    }

    [Fact]
    public async Task SetNewPassword_WithAValidToken_ChangesThePassword()
    {
        var token = await GetTokenAsync();

        var result = await _service.SetNewPasswordAsync(token, NewPassword);

        result.ShouldBe(PasswordStatus.Success);
        _accounts.Passwords[UserId].ShouldBe(NewPassword);
    }

    [Fact]
    public async Task SetNewPassword_RevokesEverySession()
    {
        var token = await GetTokenAsync();

        await _service.SetNewPasswordAsync(token, NewPassword);

        _accounts.RevokedSessions.ShouldHaveSingleItem().ShouldBe(UserId);
    }

    [Fact]
    public async Task SetNewPassword_ForgetsTheVerifiedSignIns()
    {
        var token = await GetTokenAsync();
        (await _signIns.IsVerifiedAsync(UserId, FirstSignIn)).ShouldBeTrue();

        await _service.SetNewPasswordAsync(token, NewPassword);

        (await _signIns.IsVerifiedAsync(UserId, FirstSignIn)).ShouldBeFalse();
    }

    [Fact]
    public async Task SetNewPassword_SendsAPasswordChangedNotice()
    {
        var token = await GetTokenAsync();

        await _service.SetNewPasswordAsync(token, NewPassword);

        var notice = _alerts.Sent.Last();
        notice.Type.ShouldBe(AlertType.PasswordChanged);
        notice.To.ShouldBe(Email);
    }

    [Fact]
    public async Task SetNewPassword_TheTokenCannotBeUsedTwice()
    {
        var token = await GetTokenAsync();

        await _service.SetNewPasswordAsync(token, NewPassword);
        var second = await _service.SetNewPasswordAsync(token, "another-good-password");

        second.ShouldBe(PasswordStatus.InvalidResetToken);
        _accounts.Passwords[UserId].ShouldBe(NewPassword);
    }

    [Fact]
    public async Task SetNewPassword_WithAWeakPassword_IsInvalid_AndKeepsTheToken()
    {
        var token = await GetTokenAsync();
        var tooShort = new string('a', _passwordSettings.MinLength - 1);

        var weak = await _service.SetNewPasswordAsync(token, tooShort);
        var retry = await _service.SetNewPasswordAsync(token, NewPassword);

        weak.ShouldBe(PasswordStatus.InvalidPassword);
        retry.ShouldBe(PasswordStatus.Success);
    }

    [Fact]
    public async Task SetNewPassword_WithAnUnknownToken_IsRejected()
    {
        var result = await _service.SetNewPasswordAsync("not-a-real-token", NewPassword);

        result.ShouldBe(PasswordStatus.InvalidResetToken);
        _accounts.Passwords.ShouldBeEmpty();
    }

    [Fact]
    public async Task SetNewPassword_WithAnExpiredToken_IsRejected()
    {
        var token = await GetTokenAsync();
        _clock.Advance(TimeSpan.FromMinutes(_passwordSettings.ResetTokenMinutes));

        var result = await _service.SetNewPasswordAsync(token, NewPassword);

        result.ShouldBe(PasswordStatus.InvalidResetToken);
        _accounts.Passwords.ShouldBeEmpty();
    }

    [Fact]
    public async Task SetNewPassword_WithABlankToken_IsRejected()
    {
        var result = await _service.SetNewPasswordAsync("  ", NewPassword);

        result.ShouldBe(PasswordStatus.InvalidResetToken);
    }

    [Fact]
    public async Task SetNewPassword_ForAMemberPendingDeletion_StillWorks_WithoutRestoringTheAccount()
    {
        var token = await GetTokenAsync();
        await _users.ModifyAsync(UserId, user => user.DeletedAt = _clock.UtcNow);

        var result = await _service.SetNewPasswordAsync(token, NewPassword);

        result.ShouldBe(PasswordStatus.Success);
        (await _users.GetByIdAsync(UserId))!.DeletedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task SetNewPassword_WhenTheAccountIsNoLongerAMember_IsAccountNotFound()
    {
        var token = await GetTokenAsync();
        await _users.ModifyAsync(UserId, user => user.AccessLevel = AccessLevel.Guest);

        var result = await _service.SetNewPasswordAsync(token, NewPassword);

        result.ShouldBe(PasswordStatus.AccountNotFound);
        _accounts.Passwords.ShouldBeEmpty();
    }

    [Fact]
    public async Task SetNewPassword_WhenTheNoticeFails_StillSucceeds()
    {
        var token = await GetTokenAsync();
        _alerts.FailingTypes.Add(AlertType.PasswordChanged);

        var result = await _service.SetNewPasswordAsync(token, NewPassword);

        result.ShouldBe(PasswordStatus.Success);
        _accounts.Passwords[UserId].ShouldBe(NewPassword);
    }

    [Fact]
    public async Task SetNewPassword_WhenSettingThePasswordFails_Throws_AndRevokesNothing()
    {
        var token = await GetTokenAsync();
        _accounts.SetPasswordShouldFail = true;

        await Should.ThrowAsync<InvalidOperationException>(
            () => _service.SetNewPasswordAsync(token, NewPassword));

        _accounts.RevokedSessions.ShouldBeEmpty();
    }

    #endregion

    #region Private Methods

    // The answer every reset request must return.
    private PasswordCodeResult ExpectedAnswer()
    {
        return new PasswordCodeResult(
            PasswordStatus.Success,
            _verificationSettings.PasswordResetCodeLength,
            _verificationSettings.ResendCooldownSeconds);
    }

    // Requests a code for the member and runs the queued delivery; returns the emailed code.
    private async Task<string> RequestCodeAsync()
    {
        await _service.RequestResetAsync(Email);
        await _queue.RunAllAsync(_provider);

        return _alerts.LastCode;
    }

    // Requests and confirms a code for the member; returns the reset token.
    private async Task<string> GetTokenAsync()
    {
        var code = await RequestCodeAsync();
        var result = await _service.VerifyCodeAsync(Email, code);

        return result.ResetToken!;
    }

    #endregion
}
