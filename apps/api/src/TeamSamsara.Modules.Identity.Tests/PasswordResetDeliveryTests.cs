// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/PasswordResetDeliveryTests.cs
// Version : 1.0.0
// Latest commit: feat/password-reset-service
// Author : Gerrah
// Purpose : Proves reset code delivery: members get a code, other addresses get a silent decoy,
// requests from unknown clients are flagged, and email failures behave as designed.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Tests.Fakes;
using TeamSamsara.Shared.Alert;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Tests;

public class PasswordResetDeliveryTests : IAsyncLifetime
{
    #region Fields

    private const string UserId = "user-1";
    private const string Email = "jane.doe@example.com";
    private const string UnknownEmail = "nobody@example.com";
    private const string KnownClientFingerprint = "fingerprint-a";
    private const string WrongCode = "not-the-code";

    private readonly ClientInfo _knownClient = new("203.0.113.7", "TestBrowser", KnownClientFingerprint);
    private readonly ClientInfo _unknownClient = new("198.51.100.9", "OtherBrowser", "fingerprint-z");

    private readonly InMemoryUserStore _users = new();
    private readonly InMemoryVerificationCodeStore _codes = new();
    private readonly RecordingAlertSender _alerts = new();
    private readonly FakeAccountGateway _accounts = new();
    private readonly FakeClock _clock = new();
    private readonly FakeGuardDog _guardDog = new();
    private readonly VerificationSettings _settings = new();
    private readonly VerificationCodeService _verification;
    private readonly PasswordResetDelivery _delivery;

    #endregion

    #region Constructors

    public PasswordResetDeliveryTests()
    {
        _verification = new VerificationCodeService(
            _codes, _alerts, _clock, Options.Create(_settings));

        _delivery = new PasswordResetDelivery(
            _users,
            _accounts,
            _verification,
            _guardDog,
            _alerts,
            NullLogger<PasswordResetDelivery>.Instance);

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
            KnownFingerprints = new List<KnownFingerprint>
            {
                new() { Hash = KnownClientFingerprint, LastSeenAt = _clock.UtcNow }
            }
        });
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Deliver_ForAMember_EmailsAStepUpCodeToTheAccountAddress()
    {
        await _delivery.DeliverAsync(Email, _knownClient);

        var alert = _alerts.Sent.ShouldHaveSingleItem();
        alert.To.ShouldBe(Email);
        alert.Type.ShouldBe(AlertType.StepUpCode);
        _alerts.LastCode.Length.ShouldBe(_settings.PasswordResetCodeLength);
    }

    [Fact]
    public async Task Deliver_ForAMember_FilesTheCodeUnderTheHashedEmail_NotTheUserId()
    {
        await _delivery.DeliverAsync(Email, _knownClient);

        var byEmail = await _codes.GetAsync(PasswordResetCrypto.HashEmail(Email), VerificationPurpose.PasswordReset);
        var byUser = await _codes.GetAsync(UserId, VerificationPurpose.PasswordReset);

        byEmail.ShouldNotBeNull();
        byUser.ShouldBeNull();
    }

    [Fact]
    public async Task Deliver_IgnoresCaseAndSurroundingWhitespaceInTheAddress()
    {
        await _delivery.DeliverAsync("  Jane.Doe@Example.com ", _knownClient);

        _alerts.Sent.ShouldHaveSingleItem().Type.ShouldBe(AlertType.StepUpCode);
        (await _codes.GetAsync(PasswordResetCrypto.HashEmail(Email), VerificationPurpose.PasswordReset))
            .ShouldNotBeNull();
    }

    [Fact]
    public async Task Deliver_FromAnUnknownClient_AlsoSendsAResetRequestedNotice()
    {
        await _delivery.DeliverAsync(Email, _unknownClient);

        _alerts.Sent.Count.ShouldBe(2);

        var notice = _alerts.Sent.Single(alert => alert.Type == AlertType.PasswordResetRequested);
        notice.To.ShouldBe(Email);
        notice.TemplateData[AlertTemplateKeys.IpAddress].ShouldBe(_unknownClient.IpAddress);
        notice.TemplateData[AlertTemplateKeys.UserAgent].ShouldBe(_unknownClient.UserAgent);
    }

    [Fact]
    public async Task Deliver_ForAnUnknownAddress_StoresADecoy_AndSendsNothing()
    {
        await _delivery.DeliverAsync(UnknownEmail, _knownClient);

        _alerts.Sent.ShouldBeEmpty();
        (await _codes.GetAsync(PasswordResetCrypto.HashEmail(UnknownEmail), VerificationPurpose.PasswordReset))
            .ShouldNotBeNull();
    }

    [Fact]
    public async Task Deliver_ForAnUnknownAddress_FromAnUnknownClient_SendsNoNotice()
    {
        await _delivery.DeliverAsync(UnknownEmail, _unknownClient);

        _alerts.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task Deliver_ForAGuestAccount_IsTreatedAsUnknown()
    {
        _accounts.AddAccount("guest-1", "guest@example.com");
        await _users.CreateAsync(new User
        {
            Id = "guest-1",
            AccessLevel = AccessLevel.Guest,
            CreatedAt = _clock.UtcNow
        });

        await _delivery.DeliverAsync("guest@example.com", _unknownClient);

        _alerts.Sent.ShouldBeEmpty();
        (await _codes.GetAsync(PasswordResetCrypto.HashEmail("guest@example.com"), VerificationPurpose.PasswordReset))
            .ShouldNotBeNull();
    }

    [Fact]
    public async Task Deliver_ForAnAccountWithoutAUserRecord_IsTreatedAsUnknown()
    {
        _accounts.AddAccount("orphan-1", "orphan@example.com");

        await _delivery.DeliverAsync("orphan@example.com", _unknownClient);

        _alerts.Sent.ShouldBeEmpty();
        (await _codes.GetAsync(PasswordResetCrypto.HashEmail("orphan@example.com"), VerificationPurpose.PasswordReset))
            .ShouldNotBeNull();
    }

    [Fact]
    public async Task Deliver_ForAMemberPendingDeletion_StillSendsTheCode()
    {
        await _users.ModifyAsync(UserId, user => user.DeletedAt = _clock.UtcNow);

        await _delivery.DeliverAsync(Email, _knownClient);

        _alerts.Sent.ShouldHaveSingleItem().Type.ShouldBe(AlertType.StepUpCode);
    }

    [Fact]
    public async Task Deliver_AgainWithinTheCooldown_SendsNoSecondCodeOrNotice()
    {
        await _delivery.DeliverAsync(Email, _unknownClient);
        _clock.Advance(TimeSpan.FromSeconds(20));

        await _delivery.DeliverAsync(Email, _unknownClient);

        _alerts.Sent.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Deliver_WhenTheCodeEmailFails_Throws_AndLeavesNoCodeBehind()
    {
        _alerts.FailingTypes.Add(AlertType.StepUpCode);

        await Should.ThrowAsync<InvalidOperationException>(
            () => _delivery.DeliverAsync(Email, _unknownClient));

        (await _codes.GetAsync(PasswordResetCrypto.HashEmail(Email), VerificationPurpose.PasswordReset))
            .ShouldBeNull();
        _alerts.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task Deliver_WhenTheNoticeFails_StillCompletes_AndTheCodeStaysValid()
    {
        _alerts.FailingTypes.Add(AlertType.PasswordResetRequested);

        await _delivery.DeliverAsync(Email, _unknownClient);

        _alerts.Sent.ShouldHaveSingleItem().Type.ShouldBe(AlertType.StepUpCode);
        (await _codes.GetAsync(PasswordResetCrypto.HashEmail(Email), VerificationPurpose.PasswordReset))
            .ShouldNotBeNull();
    }

    [Fact]
    public async Task Verify_AWrongCodeLooksTheSame_ForAMemberAndAnUnknownAddress()
    {
        await _delivery.DeliverAsync(Email, _knownClient);
        await _delivery.DeliverAsync(UnknownEmail, _knownClient);

        var member = await _verification.VerifyAsync(
            PasswordResetCrypto.HashEmail(Email), VerificationPurpose.PasswordReset, WrongCode);
        var unknown = await _verification.VerifyAsync(
            PasswordResetCrypto.HashEmail(UnknownEmail), VerificationPurpose.PasswordReset, WrongCode);

        member.ShouldBe(VerificationResult.Invalid);
        unknown.ShouldBe(VerificationResult.Invalid);
    }

    #endregion
}
