// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/EmailChangeDeliveryTests.cs
// Version : 1.0.0
// Latest commit: feat/email-change-service
// Author : Gerrah
// Purpose : Proves email change delivery: real codes for a free address, a silent decoy for an unavailable one.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Tests.Fakes;
using TeamSamsara.Shared.Alert;

namespace TeamSamsara.Modules.Identity.Tests;

public class EmailChangeDeliveryTests
{
    #region Fields

    private const string UserId = "user-1";
    private const string Email = "jane.doe@example.com";
    private const string NewEmail = "jane.new@example.com";
    private const string OtherUserId = "user-2";
    private const string WrongCode = "not-the-code";

    private readonly InMemoryVerificationCodeStore _codes = new();
    private readonly RecordingAlertSender _alerts = new();
    private readonly FakeAccountGateway _accounts = new();
    private readonly FakeClock _clock = new();
    private readonly VerificationSettings _settings = new();
    private readonly VerificationCodeService _verification;
    private readonly EmailChangeDelivery _delivery;

    #endregion

    #region Constructors

    public EmailChangeDeliveryTests()
    {
        _verification = new VerificationCodeService(
            _codes, _alerts, _clock, Options.Create(_settings));

        _delivery = new EmailChangeDelivery(
            _accounts, _verification, NullLogger<EmailChangeDelivery>.Instance);

        _accounts.AddAccount(UserId, Email);
    }

    #endregion

    #region Public Methods

    [Fact]
    public async Task Deliver_EmailsAStepUpCodeToTheCurrentAddress()
    {
        await _delivery.DeliverAsync(UserId, NewEmail);

        var alert = _alerts.Sent.Single(sent => sent.To == Email);
        alert.Type.ShouldBe(AlertType.StepUpCode);
        alert.TemplateData[AlertTemplateKeys.Code].Length.ShouldBe(_settings.EmailChangeCodeLength);
    }

    [Fact]
    public async Task Deliver_ForAFreeAddress_EmailsACodeToTheNewAddressToo()
    {
        await _delivery.DeliverAsync(UserId, NewEmail);

        var alert = _alerts.Sent.Single(sent => sent.To == NewEmail);
        alert.Type.ShouldBe(AlertType.StepUpCode);

        var result = await _verification.VerifyAsync(
            UserId, VerificationPurpose.EmailChangeNew, alert.TemplateData[AlertTemplateKeys.Code]);

        result.ShouldBe(VerificationResult.Valid);
    }

    [Fact]
    public async Task Deliver_TheTwoCodesAreChecked_AgainstTheirOwnPurpose()
    {
        await _delivery.DeliverAsync(UserId, NewEmail);

        var oldCode = _alerts.Sent.Single(sent => sent.To == Email).TemplateData[AlertTemplateKeys.Code];

        var result = await _verification.VerifyAsync(UserId, VerificationPurpose.EmailChangeOld, oldCode);

        result.ShouldBe(VerificationResult.Valid);
    }

    [Fact]
    public async Task Deliver_ForAnAddressAnotherAccountHolds_SendsNothingToIt()
    {
        _accounts.AddAccount(OtherUserId, NewEmail);

        await _delivery.DeliverAsync(UserId, NewEmail);

        _alerts.Sent.ShouldHaveSingleItem().To.ShouldBe(Email);
    }

    [Fact]
    public async Task Deliver_ForAnAddressAnotherAccountHolds_StoresADecoyThatBehavesLikeARealCode()
    {
        _accounts.AddAccount(OtherUserId, NewEmail);
        await _delivery.DeliverAsync(UserId, NewEmail);

        var result = await _verification.VerifyAsync(UserId, VerificationPurpose.EmailChangeNew, WrongCode);

        result.ShouldBe(VerificationResult.Invalid);
    }

    [Fact]
    public async Task Deliver_ForAFreeAddress_ALaterWrongGuess_IsAlsoInvalid()
    {
        await _delivery.DeliverAsync(UserId, NewEmail);

        var result = await _verification.VerifyAsync(UserId, VerificationPurpose.EmailChangeNew, WrongCode);

        result.ShouldBe(VerificationResult.Invalid);
    }

    [Fact]
    public async Task Deliver_ForTheMembersOwnAddress_TreatsItAsUnavailable_IgnoringCase()
    {
        await _delivery.DeliverAsync(UserId, "JANE.DOE@example.com");

        _alerts.Sent.ShouldHaveSingleItem().To.ShouldBe(Email);

        var result = await _verification.VerifyAsync(UserId, VerificationPurpose.EmailChangeNew, WrongCode);

        result.ShouldBe(VerificationResult.Invalid);
    }

    [Fact]
    public async Task Deliver_WhenEmailFails_DoesNotThrow_AndLeavesNoUsableCode()
    {
        _alerts.ShouldFail = true;

        await Should.NotThrowAsync(() => _delivery.DeliverAsync(UserId, NewEmail));

        var old = await _verification.VerifyAsync(UserId, VerificationPurpose.EmailChangeOld, WrongCode);
        var added = await _verification.VerifyAsync(UserId, VerificationPurpose.EmailChangeNew, WrongCode);

        old.ShouldBe(VerificationResult.NotFound);
        added.ShouldBe(VerificationResult.NotFound);
    }

    [Fact]
    public async Task Deliver_ForAMissingAccount_DoesNothing()
    {
        await _delivery.DeliverAsync("missing-user", NewEmail);

        _alerts.Sent.ShouldBeEmpty();
    }

    #endregion
}
