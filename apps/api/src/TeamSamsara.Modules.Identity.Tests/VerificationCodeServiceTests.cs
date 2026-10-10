// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/VerificationCodeServiceTests.cs
// Version : 1.3.0
// Latest commit: feat/email-change-foundation
// Author : Gerrah
// Purpose : Proves the verification code rules: length per purpose, cooldown, expiry, attempt limit, single use, and decoys.

using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Tests.Fakes;
using TeamSamsara.Shared.Alert;

namespace TeamSamsara.Modules.Identity.Tests;

public class VerificationCodeServiceTests
{
    #region Fields

    private const string UserId = "user-1";
    private const string Email = "member@example.com";
    private const string NewEmail = "new.member@example.com";
    private const string WrongCode = "not-the-code";
    private const string OtherUserId = "user-2";

    private readonly InMemoryVerificationCodeStore _store = new();
    private readonly RecordingAlertSender _alerts = new();
    private readonly FakeClock _clock = new();
    private readonly VerificationSettings _settings = new();
    private readonly VerificationCodeService _service;

    #endregion

    #region Constructors

    public VerificationCodeServiceTests()
    {
        _service = new VerificationCodeService(_store, _alerts, _clock, Options.Create(_settings));
    }

    #endregion

    #region Public Methods

    [Fact]
    public async Task Issue_Registration_SendsCodeOfConfiguredLength()
    {
        var result = await _service.IssueAsync(UserId, VerificationPurpose.Registration, Email);

        result.Status.ShouldBe(VerificationIssueStatus.Sent);
        result.CodeLength.ShouldBe(_settings.RegistrationCodeLength);

        var alert = _alerts.Sent.ShouldHaveSingleItem();
        alert.To.ShouldBe(Email);
        alert.Type.ShouldBe(AlertType.RegistrationCode);
        _alerts.LastCode.Length.ShouldBe(_settings.RegistrationCodeLength);
        _alerts.LastCode.ShouldAllBe(character => char.IsDigit(character));
    }

    [Fact]
    public async Task Issue_StepUp_UsesItsOwnLengthAndAlert()
    {
        var result = await _service.IssueAsync(UserId, VerificationPurpose.StepUp, Email);

        result.CodeLength.ShouldBe(_settings.StepUpCodeLength);
        _alerts.Sent.ShouldHaveSingleItem().Type.ShouldBe(AlertType.StepUpCode);
        _alerts.LastCode.Length.ShouldBe(_settings.StepUpCodeLength);
    }

    [Fact]
    public async Task Issue_LoginChallenge_ReusesTheStepUpAlert()
    {
        var result = await _service.IssueAsync(UserId, VerificationPurpose.LoginChallenge, Email);

        result.CodeLength.ShouldBe(_settings.LoginChallengeCodeLength);
        _alerts.Sent.ShouldHaveSingleItem().Type.ShouldBe(AlertType.StepUpCode);
    }

    [Fact]
    public async Task Issue_PasswordReset_UsesItsOwnLengthAndTheStepUpAlert()
    {
        var result = await _service.IssueAsync(UserId, VerificationPurpose.PasswordReset, Email);

        result.Status.ShouldBe(VerificationIssueStatus.Sent);
        result.CodeLength.ShouldBe(_settings.PasswordResetCodeLength);
        _alerts.Sent.ShouldHaveSingleItem().Type.ShouldBe(AlertType.StepUpCode);
        _alerts.LastCode.Length.ShouldBe(_settings.PasswordResetCodeLength);
    }

    [Fact]
    public async Task Issue_PasswordReset_DoesNotDisturbAPendingStepUpCode()
    {
        await _service.IssueAsync(UserId, VerificationPurpose.StepUp, Email);
        var stepUpCode = _alerts.LastCode;

        var reset = await _service.IssueAsync(UserId, VerificationPurpose.PasswordReset, Email);
        var verification = await _service.VerifyAsync(UserId, VerificationPurpose.StepUp, stepUpCode);

        reset.Status.ShouldBe(VerificationIssueStatus.Sent);
        _alerts.Sent.Count.ShouldBe(2);
        verification.ShouldBe(VerificationResult.Valid);
    }

    [Theory]
    [InlineData(VerificationPurpose.EmailChangeOld)]
    [InlineData(VerificationPurpose.EmailChangeNew)]
    public async Task Issue_EmailChange_UsesTheEmailChangeLengthAndTheStepUpAlert(VerificationPurpose purpose)
    {
        var result = await _service.IssueAsync(UserId, purpose, Email);

        result.Status.ShouldBe(VerificationIssueStatus.Sent);
        result.CodeLength.ShouldBe(_settings.EmailChangeCodeLength);
        _alerts.Sent.ShouldHaveSingleItem().Type.ShouldBe(AlertType.StepUpCode);
        _alerts.LastCode.Length.ShouldBe(_settings.EmailChangeCodeLength);
    }

    [Fact]
    public async Task Issue_EmailChange_OldAndNewCodesAreIndependent()
    {
        await _service.IssueAsync(UserId, VerificationPurpose.EmailChangeOld, Email);
        var oldCode = _alerts.LastCode;
        await _service.IssueAsync(UserId, VerificationPurpose.EmailChangeNew, NewEmail);
        var newCode = _alerts.LastCode;

        var newResult = await _service.VerifyAsync(UserId, VerificationPurpose.EmailChangeNew, newCode);
        var oldResult = await _service.VerifyAsync(UserId, VerificationPurpose.EmailChangeOld, oldCode);

        _alerts.Sent.Select(sent => sent.To).ShouldBe(new[] { Email, NewEmail });
        newResult.ShouldBe(VerificationResult.Valid);
        oldResult.ShouldBe(VerificationResult.Valid);
    }

    [Fact]
    public async Task Issue_AgainTooSoon_ReportsCooldownAndSendsNothing()
    {
        await _service.IssueAsync(UserId, VerificationPurpose.Registration, Email);
        _clock.Advance(TimeSpan.FromSeconds(20));

        var result = await _service.IssueAsync(UserId, VerificationPurpose.Registration, Email);

        result.Status.ShouldBe(VerificationIssueStatus.CooldownActive);
        result.RetryAfterSeconds.ShouldBe(_settings.ResendCooldownSeconds - 20);
        _alerts.Sent.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Issue_AfterCooldown_SendsAFreshCode()
    {
        await _service.IssueAsync(UserId, VerificationPurpose.Registration, Email);
        _clock.Advance(TimeSpan.FromSeconds(_settings.ResendCooldownSeconds + 1));

        var result = await _service.IssueAsync(UserId, VerificationPurpose.Registration, Email);

        result.Status.ShouldBe(VerificationIssueStatus.Sent);
        _alerts.Sent.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Issue_WhenEmailFails_Throws_AndDiscardsTheCodeSoRetryIsImmediate()
    {
        _alerts.ShouldFail = true;

        await Should.ThrowAsync<InvalidOperationException>(
            () => _service.IssueAsync(UserId, VerificationPurpose.Registration, Email));

        (await _store.GetAsync(UserId, VerificationPurpose.Registration)).ShouldBeNull();

        _alerts.ShouldFail = false;
        var retry = await _service.IssueAsync(UserId, VerificationPurpose.Registration, Email);

        retry.Status.ShouldBe(VerificationIssueStatus.Sent);
    }

    [Fact]
    public async Task Issue_StoresOnlyAHash_NeverTheCode()
    {
        await _service.IssueAsync(UserId, VerificationPurpose.Registration, Email);

        var stored = await _store.GetAsync(UserId, VerificationPurpose.Registration);

        stored.ShouldNotBeNull();
        stored.CodeHash.ShouldNotContain(_alerts.LastCode);
        stored.CodeHash.ShouldNotBe(_alerts.LastCode);
    }

    [Fact]
    public async Task Verify_CorrectCode_IsValid_AndCannotBeUsedTwice()
    {
        await _service.IssueAsync(UserId, VerificationPurpose.Registration, Email);
        var code = _alerts.LastCode;

        var first = await _service.VerifyAsync(UserId, VerificationPurpose.Registration, code);
        var second = await _service.VerifyAsync(UserId, VerificationPurpose.Registration, code);

        first.ShouldBe(VerificationResult.Valid);
        second.ShouldBe(VerificationResult.NotFound);
    }

    [Fact]
    public async Task Verify_IgnoresSurroundingWhitespace()
    {
        await _service.IssueAsync(UserId, VerificationPurpose.Registration, Email);

        var result = await _service.VerifyAsync(
            UserId, VerificationPurpose.Registration, "  " + _alerts.LastCode + " ");

        result.ShouldBe(VerificationResult.Valid);
    }

    [Fact]
    public async Task Verify_WrongCode_IsInvalid_AndTheRealCodeStillWorks()
    {
        await _service.IssueAsync(UserId, VerificationPurpose.Registration, Email);

        var wrong = await _service.VerifyAsync(UserId, VerificationPurpose.Registration, WrongCode);
        var right = await _service.VerifyAsync(UserId, VerificationPurpose.Registration, _alerts.LastCode);

        wrong.ShouldBe(VerificationResult.Invalid);
        right.ShouldBe(VerificationResult.Valid);
    }

    [Fact]
    public async Task Verify_WithNoPendingCode_IsNotFound()
    {
        var result = await _service.VerifyAsync(UserId, VerificationPurpose.Registration, "123456");

        result.ShouldBe(VerificationResult.NotFound);
    }

    [Fact]
    public async Task Verify_ExpiredCode_IsExpired_AndIsRemoved()
    {
        await _service.IssueAsync(UserId, VerificationPurpose.Registration, Email);
        var code = _alerts.LastCode;
        _clock.Advance(TimeSpan.FromMinutes(_settings.ExpiryMinutes));

        var result = await _service.VerifyAsync(UserId, VerificationPurpose.Registration, code);
        var again = await _service.VerifyAsync(UserId, VerificationPurpose.Registration, code);

        result.ShouldBe(VerificationResult.Expired);
        again.ShouldBe(VerificationResult.NotFound);
    }

    [Fact]
    public async Task Verify_TooManyWrongGuesses_BurnsTheCode_EvenForTheRealOne()
    {
        await _service.IssueAsync(UserId, VerificationPurpose.Registration, Email);
        var code = _alerts.LastCode;

        for (var attempt = 1; attempt < _settings.MaxFailedAttempts; attempt++)
        {
            var result = await _service.VerifyAsync(UserId, VerificationPurpose.Registration, WrongCode);
            result.ShouldBe(VerificationResult.Invalid);
        }

        var final = await _service.VerifyAsync(UserId, VerificationPurpose.Registration, WrongCode);
        var real = await _service.VerifyAsync(UserId, VerificationPurpose.Registration, code);

        final.ShouldBe(VerificationResult.TooManyAttempts);
        real.ShouldBe(VerificationResult.NotFound);
    }

    [Fact]
    public async Task Verify_CodesAreSeparatePerPurpose()
    {
        await _service.IssueAsync(UserId, VerificationPurpose.Registration, Email);

        var result = await _service.VerifyAsync(UserId, VerificationPurpose.StepUp, _alerts.LastCode);

        result.ShouldBe(VerificationResult.NotFound);
    }

    [Fact]
    public async Task IssueDecoy_SendsNothing_AndReportsSent()
    {
        var result = await _service.IssueDecoyAsync(UserId, VerificationPurpose.PasswordReset);

        result.Status.ShouldBe(VerificationIssueStatus.Sent);
        _alerts.Sent.ShouldBeEmpty();
        (await _store.GetAsync(UserId, VerificationPurpose.PasswordReset)).ShouldNotBeNull();
    }

    [Fact]
    public async Task IssueDecoy_GivesTheSameAnswerAndStoredShapeAsARealCode()
    {
        var real = await _service.IssueAsync(UserId, VerificationPurpose.PasswordReset, Email);
        var decoy = await _service.IssueDecoyAsync(OtherUserId, VerificationPurpose.PasswordReset);

        var realStored = await _store.GetAsync(UserId, VerificationPurpose.PasswordReset);
        var decoyStored = await _store.GetAsync(OtherUserId, VerificationPurpose.PasswordReset);

        decoy.ShouldBe(real);
        realStored.ShouldNotBeNull();
        decoyStored.ShouldNotBeNull();
        decoyStored.CreatedAt.ShouldBe(realStored.CreatedAt);
        decoyStored.ExpiresAt.ShouldBe(realStored.ExpiresAt);
        decoyStored.CodeHash.Length.ShouldBe(realStored.CodeHash.Length);
        decoyStored.Salt.Length.ShouldBe(realStored.Salt.Length);
    }

    [Fact]
    public async Task IssueDecoy_UsesThePurposeCodeLength()
    {
        var result = await _service.IssueDecoyAsync(UserId, VerificationPurpose.PasswordReset);

        result.CodeLength.ShouldBe(_settings.PasswordResetCodeLength);
    }

    [Fact]
    public async Task IssueDecoy_AgainTooSoon_ReportsTheSameCooldownAsARealCode()
    {
        await _service.IssueDecoyAsync(UserId, VerificationPurpose.PasswordReset);
        _clock.Advance(TimeSpan.FromSeconds(20));

        var result = await _service.IssueDecoyAsync(UserId, VerificationPurpose.PasswordReset);

        result.Status.ShouldBe(VerificationIssueStatus.CooldownActive);
        result.RetryAfterSeconds.ShouldBe(_settings.ResendCooldownSeconds - 20);
    }

    [Fact]
    public async Task IssueDecoy_WhenEmailDeliveryIsDown_StillSucceeds()
    {
        _alerts.ShouldFail = true;

        var result = await _service.IssueDecoyAsync(UserId, VerificationPurpose.PasswordReset);

        result.Status.ShouldBe(VerificationIssueStatus.Sent);
        (await _store.GetAsync(UserId, VerificationPurpose.PasswordReset)).ShouldNotBeNull();
    }

    [Fact]
    public async Task Verify_AgainstADecoy_WrongCodeIsInvalid_NotNotFound()
    {
        await _service.IssueDecoyAsync(UserId, VerificationPurpose.PasswordReset);

        var result = await _service.VerifyAsync(UserId, VerificationPurpose.PasswordReset, WrongCode);

        result.ShouldBe(VerificationResult.Invalid);
    }

    [Fact]
    public async Task Verify_AgainstADecoy_TooManyGuessesBurnsIt()
    {
        await _service.IssueDecoyAsync(UserId, VerificationPurpose.PasswordReset);

        for (var attempt = 1; attempt < _settings.MaxFailedAttempts; attempt++)
        {
            var result = await _service.VerifyAsync(UserId, VerificationPurpose.PasswordReset, WrongCode);
            result.ShouldBe(VerificationResult.Invalid);
        }

        var final = await _service.VerifyAsync(UserId, VerificationPurpose.PasswordReset, WrongCode);
        var after = await _service.VerifyAsync(UserId, VerificationPurpose.PasswordReset, WrongCode);

        final.ShouldBe(VerificationResult.TooManyAttempts);
        after.ShouldBe(VerificationResult.NotFound);
    }

    [Fact]
    public async Task Verify_AgainstADecoy_ExpiresLikeARealCode()
    {
        await _service.IssueDecoyAsync(UserId, VerificationPurpose.PasswordReset);
        _clock.Advance(TimeSpan.FromMinutes(_settings.ExpiryMinutes));

        var result = await _service.VerifyAsync(UserId, VerificationPurpose.PasswordReset, WrongCode);

        result.ShouldBe(VerificationResult.Expired);
    }

    #endregion
}
