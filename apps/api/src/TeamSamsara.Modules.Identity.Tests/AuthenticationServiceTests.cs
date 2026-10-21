// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/AuthenticationServiceTests.cs
// Version : 1.2.0
// Latest commit: task/notifications-api
// Author : Gerrah
// Purpose : Proves login: a recognized client is verified straight away, an unrecognized one
// must confirm a code (and the member is told, by email and in the inbox), deleted accounts are restored only after a
// confirmed code, only up to the end of the recovery window, and never while a purge holds them.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Tests.Fakes;
using TeamSamsara.Shared.Alert;
using TeamSamsara.Shared.Context;
using TeamSamsara.Shared.Notification;

namespace TeamSamsara.Modules.Identity.Tests;

public class AuthenticationServiceTests
{
    #region Fields

    private const string UserId = "user-1";
    private const string Email = "member@example.com";
    private const long AuthTime = 1_700_000_000;

    private static readonly ClientInfo _knownClient = new("203.0.113.7", "TestBrowser", "fingerprint-a");
    private static readonly ClientInfo _newClient = new("198.51.100.9", "OtherBrowser", "fingerprint-b");
    private static readonly ClientInfo _thirdClient = new("192.0.2.1", "ThirdBrowser", "fingerprint-c");

    private readonly InMemoryUserStore _users = new();
    private readonly InMemoryVerificationCodeStore _codes = new();
    private readonly InMemoryCacheService _cache = new();
    private readonly RecordingAlertSender _alerts = new();
    private readonly RecordingNotificationSender _notifications = new();
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
            _notifications,
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
        _alerts.Sent[0].Type.ShouldBe(AlertType.LoginCode);

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
        _alerts.Sent.ShouldHaveSingleItem().Type.ShouldBe(AlertType.LoginCode);
    }

    [Fact]
    public async Task Check_FromAnUnrecognizedClient_PutsANoticeInTheInbox()
    {
        _users.Seed(NewMemberWhoKnows(_knownClient));
        _guardDog.Current = _newClient;

        await _service.CheckSignInAsync(UserId, AuthTime);

        var request = _notifications.Requests.ShouldHaveSingleItem();
        request.RecipientId.ShouldBe(UserId);
        request.Key.ShouldBe("security.new-login");
        request.Data.ShouldNotBeNull();
        request.Data["ipAddress"].ShouldBe(_newClient.IpAddress);
        request.Data["userAgent"].ShouldBe(_newClient.UserAgent);
        request.DedupeKey.ShouldNotBeNull();
        request.DedupeKey.ShouldMatch(@"^new-login:[0-9a-f]{32}:20260101\z");
    }

    [Fact]
    public async Task Check_FromARecognizedClient_PutsNothingInTheInbox()
    {
        _users.Seed(NewMemberWhoKnows(_knownClient));

        await _service.CheckSignInAsync(UserId, AuthTime);

        _notifications.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task Check_AgainDuringTheCooldown_PutsNoSecondNoticeInTheInbox()
    {
        _users.Seed(NewMemberWhoKnows(_knownClient));
        _guardDog.Current = _newClient;
        await _service.CheckSignInAsync(UserId, AuthTime);

        await _service.CheckSignInAsync(UserId, AuthTime);

        _notifications.Requests.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Resend_PutsNoNoticeInTheInbox()
    {
        _users.Seed(NewMemberWhoKnows(_knownClient));
        _guardDog.Current = _newClient;
        await _service.CheckSignInAsync(UserId, AuthTime);

        _clock.Advance(TimeSpan.FromSeconds(_verificationSettings.ResendCooldownSeconds + 1));
        await _service.ResendChallengeAsync(UserId);

        _notifications.Requests.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Check_FromTheSameClientLaterTheSameDay_UsesTheSameDedupeKey()
    {
        _users.Seed(NewMemberWhoKnows(_knownClient));
        _guardDog.Current = _newClient;
        await _service.CheckSignInAsync(UserId, AuthTime);

        _clock.Advance(TimeSpan.FromSeconds(_verificationSettings.ResendCooldownSeconds + 1));
        await _service.CheckSignInAsync(UserId, AuthTime);

        _notifications.Requests.Count.ShouldBe(2);
        _notifications.Requests[1].DedupeKey.ShouldBe(_notifications.Requests[0].DedupeKey);
    }

    [Fact]
    public async Task Check_FromTheSameClientTheNextDay_UsesAnotherDedupeKey()
    {
        _users.Seed(NewMemberWhoKnows(_knownClient));
        _guardDog.Current = _newClient;
        await _service.CheckSignInAsync(UserId, AuthTime);

        _clock.Advance(TimeSpan.FromDays(1));
        await _service.CheckSignInAsync(UserId, AuthTime);

        _notifications.Requests.Count.ShouldBe(2);
        _notifications.Requests[1].DedupeKey.ShouldNotBe(_notifications.Requests[0].DedupeKey);
    }

    [Fact]
    public async Task Check_FromAnotherUnrecognizedClient_UsesAnotherDedupeKey()
    {
        _users.Seed(NewMemberWhoKnows(_knownClient));
        _guardDog.Current = _newClient;
        await _service.CheckSignInAsync(UserId, AuthTime);

        _clock.Advance(TimeSpan.FromSeconds(_verificationSettings.ResendCooldownSeconds + 1));
        _guardDog.Current = _thirdClient;
        await _service.CheckSignInAsync(UserId, AuthTime);

        _notifications.Requests.Count.ShouldBe(2);
        _notifications.Requests[1].DedupeKey.ShouldNotBe(_notifications.Requests[0].DedupeKey);
    }

    [Fact]
    public async Task Check_FromAClientWithAVeryLongUserAgent_KeepsTheNoticeWithinTheContract()
    {
        _users.Seed(NewMemberWhoKnows(_knownClient));
        _guardDog.Current = new ClientInfo("198.51.100.9", new string('x', 600), "fingerprint-b");

        await _service.CheckSignInAsync(UserId, AuthTime);

        var request = _notifications.Requests.ShouldHaveSingleItem();
        request.Data.ShouldNotBeNull();
        request.Data["userAgent"].ShouldBe(new string('x', 500));
    }

    [Fact]
    public async Task Check_WhenTheInboxFails_StillChallenges_AndEmailsTheMember()
    {
        _users.Seed(NewMemberWhoKnows(_knownClient));
        _guardDog.Current = _newClient;
        _notifications.FailureToThrow = new InvalidOperationException("Simulated inbox failure.");

        var result = await _service.CheckSignInAsync(UserId, AuthTime);

        result.Status.ShouldBe(AuthenticationStatus.ChallengeRequired);
        _alerts.Sent.Select(alert => alert.Type).ShouldBe(new[] { AlertType.LoginCode, AlertType.NewLoginAttempt });
    }

    [Theory]
    [InlineData(NotificationStatus.Failed)]
    [InlineData(NotificationStatus.Conflict)]
    [InlineData(NotificationStatus.Duplicate)]
    public async Task Check_WhenTheInboxReportsAnythingButStored_StillChallenges(NotificationStatus status)
    {
        _users.Seed(NewMemberWhoKnows(_knownClient));
        _guardDog.Current = _newClient;
        _notifications.StatusToReturn = status;

        var result = await _service.CheckSignInAsync(UserId, AuthTime);

        result.Status.ShouldBe(AuthenticationStatus.ChallengeRequired);
        _notifications.Requests.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Check_WhenTheNewLoginEmailFails_StillPutsANoticeInTheInbox()
    {
        _users.Seed(NewMemberWhoKnows(_knownClient));
        _guardDog.Current = _newClient;
        _alerts.FailingTypes.Add(AlertType.NewLoginAttempt);

        var result = await _service.CheckSignInAsync(UserId, AuthTime);

        result.Status.ShouldBe(AuthenticationStatus.ChallengeRequired);
        _notifications.Requests.ShouldHaveSingleItem();
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
        _alerts.Sent.Count(alert => alert.Type == AlertType.LoginCode).ShouldBe(2);
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

    [Fact]
    public async Task ADeletedAccount_ExactlyOneWindowOld_CanStillBeRestored()
    {
        var user = NewMemberWhoKnows(_knownClient);
        user.DeletedAt = _clock.UtcNow.AddDays(-_settings.RecoveryWindowDays);
        _users.Seed(user);

        var check = await _service.CheckSignInAsync(UserId, AuthTime);
        var status = await _service.ConfirmChallengeAsync(UserId, AuthTime, _alerts.LastCode);

        check.Status.ShouldBe(AuthenticationStatus.ChallengeRequired);
        status.ShouldBe(AuthenticationStatus.Authenticated);
        (await _users.GetByIdAsync(UserId))!.DeletedAt.ShouldBeNull();
    }

    [Fact]
    public async Task ADeletedAccount_OneSecondPastTheWindow_CannotBeRestored()
    {
        var user = NewMemberWhoKnows(_knownClient);
        user.DeletedAt = _clock.UtcNow.AddDays(-_settings.RecoveryWindowDays).AddSeconds(-1);
        _users.Seed(user);

        var check = await _service.CheckSignInAsync(UserId, AuthTime);

        check.Status.ShouldBe(AuthenticationStatus.AccountNotFound);
        (await _users.GetByIdAsync(UserId))!.DeletedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Confirm_WhileAPurgeHoldsTheAccount_RefusesToRestore_AndChangesNothing()
    {
        var user = NewMemberWhoKnows(_knownClient);
        user.DeletedAt = _clock.UtcNow.AddDays(-5);
        user.PurgeLeaseUntil = _clock.UtcNow.AddMinutes(10);
        _users.Seed(user);
        _guardDog.Current = _newClient;
        await _service.CheckSignInAsync(UserId, AuthTime);

        var status = await _service.ConfirmChallengeAsync(UserId, AuthTime, _alerts.LastCode);

        status.ShouldBe(AuthenticationStatus.AccountNotFound);

        var stored = await _users.GetByIdAsync(UserId);
        stored.ShouldNotBeNull();
        stored.DeletedAt.ShouldBe(user.DeletedAt);
        stored.PurgeLeaseUntil.ShouldBe(user.PurgeLeaseUntil);
        stored.VerifiedSignIns.ShouldBeEmpty();
        stored.KnownFingerprints.ShouldNotContain(known => known.Hash == _newClient.Fingerprint);
        (await _signIns.IsVerifiedAsync(UserId, AuthTime)).ShouldBeFalse();
    }

    [Fact]
    public async Task Restoring_ClearsTheDeletionMarkerAndAnyLapsedLease_AndVerifiesTheSignIn_Together()
    {
        var user = NewMemberWhoKnows(_knownClient);
        user.DeletedAt = _clock.UtcNow.AddDays(-5);
        user.PurgeLeaseUntil = _clock.UtcNow.AddMinutes(-1);
        _users.Seed(user);
        _guardDog.Current = _newClient;
        await _service.CheckSignInAsync(UserId, AuthTime);

        var status = await _service.ConfirmChallengeAsync(UserId, AuthTime, _alerts.LastCode);

        status.ShouldBe(AuthenticationStatus.Authenticated);

        var stored = await _users.GetByIdAsync(UserId);
        stored.ShouldNotBeNull();
        stored.DeletedAt.ShouldBeNull();
        stored.PurgeLeaseUntil.ShouldBeNull();
        stored.VerifiedSignIns.ShouldHaveSingleItem().ShouldBe(AuthTime);
        stored.KnownFingerprints.ShouldContain(known => known.Hash == _newClient.Fingerprint);
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
