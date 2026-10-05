// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/AuthenticationServiceTests.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : Proves login: a recognized client is verified straight away, an unrecognized one
// must confirm a code (and the member is told), deleted accounts are restored only after a
// confirmed code and only within the recovery window.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Tests.Fakes;
using TeamSamsara.Shared.Alert;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Tests;

public class AuthenticationServiceTests
{
    #region Fields

    private const string UserId = "user-1";
    private const string Email = "member@example.com";
    private const long AuthTime = 1_700_000_000;

    private static readonly ClientInfo _knownClient = new("203.0.113.7", "TestBrowser", "fingerprint-a");
    private static readonly ClientInfo _newClient = new("198.51.100.9", "OtherBrowser", "fingerprint-b");

    private readonly InMemoryUserStore _users = new();
    private readonly InMemoryVerificationCodeStore _codes = new();
    private readonly InMemoryCacheService _cache = new();
    private readonly RecordingAlertSender _alerts = new();
    private readonly FakeAccountGateway _accounts = new();
    private readonly FakeGuardDog _guardDog = new();
    private readonly FakeClock _clock = new();
    private readonly VerificationSettings _verificationSettings = new();
    private readonly AuthenticationSettings _settings = new();
    private readonly SignInTracker _signIns;
    private readonly AuthenticationService _service;

    #endregion

    #region Constructors

    public AuthenticationServiceTests()
    {
        _signIns = new SignInTracker(_users, _cache, Options.Create(_settings));

        var verification = new VerificationCodeService(
            _codes, _alerts, _clock, Options.Create(_verificationSettings));

        _service = new AuthenticationService(
            _users,
            _accounts,
            verification,
            _guardDog,
            _signIns,
            _alerts,
            _clock,
            Options.Create(_settings),
            NullLogger<AuthenticationService>.Instance);

        _accounts.AddAccount(UserId, Email);
        _guardDog.Current = _knownClient;
    }

    #endregion

    #region Public Methods

    [Fact]
    public async Task Check_ForAnUnknownAccount_ReportsAccountNotFound()
    {
        var result = await _service.CheckSignInAsync("nobody", AuthTime);

        result.Status.ShouldBe(AuthenticationStatus.AccountNotFound);
    }

    [Fact]
    public async Task Check_ForAGuest_ReportsRegistrationIncomplete()
    {
        _users.Seed(NewUser(AccessLevel.Guest));

        var result = await _service.CheckSignInAsync(UserId, AuthTime);

        result.Status.ShouldBe(AuthenticationStatus.RegistrationIncomplete);
    }

    [Fact]
    public async Task Check_FromARecognizedClient_VerifiesTheSignIn_WithoutAnyEmail()
    {
        _users.Seed(NewMemberWhoKnows(_knownClient));

        var result = await _service.CheckSignInAsync(UserId, AuthTime);

        result.Status.ShouldBe(AuthenticationStatus.Authenticated);
        (await _signIns.IsVerifiedAsync(UserId, AuthTime)).ShouldBeTrue();
        _alerts.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task Check_FromAnUnrecognizedClient_RequiresAChallenge_AndTellsTheMember()
    {
        _users.Seed(NewMemberWhoKnows(_knownClient));
        _guardDog.Current = _newClient;

        var result = await _service.CheckSignInAsync(UserId, AuthTime);

        result.Status.ShouldBe(AuthenticationStatus.ChallengeRequired);
        result.CodeLength.ShouldBe(_verificationSettings.LoginChallengeCodeLength);
        (await _signIns.IsVerifiedAsync(UserId, AuthTime)).ShouldBeFalse();

        _alerts.Sent.Count.ShouldBe(2);
        _alerts.Sent[0].Type.ShouldBe(AlertType.StepUpCode);

        var notice = _alerts.Sent[1];
        notice.Type.ShouldBe(AlertType.NewLoginAttempt);
        notice.To.ShouldBe(Email);
        notice.TemplateData[AlertTemplateKeys.IpAddress].ShouldBe(_newClient.IpAddress);
        notice.TemplateData[AlertTemplateKeys.UserAgent].ShouldBe(_newClient.UserAgent);
    }

    [Fact]
    public async Task Check_AgainDuringTheCooldown_StillChallenges_WithoutAnotherEmail()
    {
        _users.Seed(NewMemberWhoKnows(_knownClient));
        _guardDog.Current = _newClient;
        await _service.CheckSignInAsync(UserId, AuthTime);

        var second = await _service.CheckSignInAsync(UserId, AuthTime);

        second.Status.ShouldBe(AuthenticationStatus.ChallengeRequired);
        second.RetryAfterSeconds.ShouldBeGreaterThan(0);
        _alerts.Sent.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Check_WhenTheNewLoginNoticeFails_StillChallenges()
    {
        _users.Seed(NewMemberWhoKnows(_knownClient));
        _guardDog.Current = _newClient;
        _alerts.FailingTypes.Add(AlertType.NewLoginAttempt);

        var result = await _service.CheckSignInAsync(UserId, AuthTime);

        result.Status.ShouldBe(AuthenticationStatus.ChallengeRequired);
        _alerts.Sent.ShouldHaveSingleItem().Type.ShouldBe(AlertType.StepUpCode);
    }

    [Fact]
    public async Task Confirm_WithTheRightCode_VerifiesTheSignIn_AndRemembersTheClient()
    {
        _users.Seed(NewMemberWhoKnows(_knownClient));
        _guardDog.Current = _newClient;
        await _service.CheckSignInAsync(UserId, AuthTime);

        var status = await _service.ConfirmChallengeAsync(UserId, AuthTime, _alerts.LastCode);

        status.ShouldBe(AuthenticationStatus.Authenticated);
        (await _signIns.IsVerifiedAsync(UserId, AuthTime)).ShouldBeTrue();

        var user = await _users.GetByIdAsync(UserId);
        user.ShouldNotBeNull();
        user.KnownFingerprints.ShouldContain(known => known.Hash == _newClient.Fingerprint);
    }

    [Fact]
    public async Task Confirm_WithAWrongCode_DoesNotVerify()
    {
        _users.Seed(NewMemberWhoKnows(_knownClient));
        _guardDog.Current = _newClient;
        await _service.CheckSignInAsync(UserId, AuthTime);

        var status = await _service.ConfirmChallengeAsync(UserId, AuthTime, "not-the-code");

        status.ShouldBe(AuthenticationStatus.InvalidCode);
        (await _signIns.IsVerifiedAsync(UserId, AuthTime)).ShouldBeFalse();
    }

    [Fact]
    public async Task Confirm_WithoutAChallenge_ReportsNoPendingCode()
    {
        _users.Seed(NewMemberWhoKnows(_knownClient));

        var status = await _service.ConfirmChallengeAsync(UserId, AuthTime, "123456");

        status.ShouldBe(AuthenticationStatus.NoPendingCode);
    }

    [Fact]
    public async Task Resend_DuringTheCooldown_ReportsCooldown_AfterwardsSendsAnotherCode_WithoutAnotherNotice()
    {
        _users.Seed(NewMemberWhoKnows(_knownClient));
        _guardDog.Current = _newClient;
        await _service.CheckSignInAsync(UserId, AuthTime);

        var early = await _service.ResendChallengeAsync(UserId);

        _clock.Advance(TimeSpan.FromSeconds(_verificationSettings.ResendCooldownSeconds + 1));
        var later = await _service.ResendChallengeAsync(UserId);

        early.Status.ShouldBe(AuthenticationStatus.CooldownActive);
        later.Status.ShouldBe(AuthenticationStatus.ChallengeRequired);
        _alerts.Sent.Count(alert => alert.Type == AlertType.StepUpCode).ShouldBe(2);
        _alerts.Sent.Count(alert => alert.Type == AlertType.NewLoginAttempt).ShouldBe(1);
    }

    [Fact]
    public async Task ADeletedAccount_WithinTheRecoveryWindow_IsChallengedEvenFromAKnownClient_ThenRestored()
    {
        var user = NewMemberWhoKnows(_knownClient);
        user.DeletedAt = _clock.UtcNow.AddDays(-5);
        _users.Seed(user);

        var check = await _service.CheckSignInAsync(UserId, AuthTime);
        var status = await _service.ConfirmChallengeAsync(UserId, AuthTime, _alerts.LastCode);

        check.Status.ShouldBe(AuthenticationStatus.ChallengeRequired);
        status.ShouldBe(AuthenticationStatus.Authenticated);
        (await _users.GetByIdAsync(UserId))!.DeletedAt.ShouldBeNull();
        (await _signIns.IsVerifiedAsync(UserId, AuthTime)).ShouldBeTrue();
    }

    [Fact]
    public async Task ADeletedAccount_PastTheRecoveryWindow_CannotSignInAtAll()
    {
        var user = NewMemberWhoKnows(_knownClient);
        user.DeletedAt = _clock.UtcNow.AddDays(-(_settings.RecoveryWindowDays + 1));
        _users.Seed(user);

        var check = await _service.CheckSignInAsync(UserId, AuthTime);
        var confirm = await _service.ConfirmChallengeAsync(UserId, AuthTime, "123456");
        var resend = await _service.ResendChallengeAsync(UserId);

        check.Status.ShouldBe(AuthenticationStatus.AccountNotFound);
        confirm.ShouldBe(AuthenticationStatus.AccountNotFound);
        resend.Status.ShouldBe(AuthenticationStatus.AccountNotFound);
    }

    #endregion

    #region Private Methods

    private User NewUser(AccessLevel accessLevel)
    {
        return new User { Id = UserId, AccessLevel = accessLevel, CreatedAt = _clock.UtcNow };
    }

    // A member who has logged in from the given client before
    private User NewMemberWhoKnows(ClientInfo client)
    {
        var user = NewUser(AccessLevel.Member);

        user.KnownFingerprints.Add(new KnownFingerprint
        {
            Hash = client.Fingerprint,
            LastSeenAt = _clock.UtcNow
        });

        return user;
    }

    #endregion
}
