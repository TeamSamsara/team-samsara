// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/EmailChangeServiceTests.cs
// Version : 1.2.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Proves email change: a taken or unchanged address is refused at once without a cooldown, each code is accepted on its own, a completed change ends every session, and the log never holds a full address or a code.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Tests.Fakes;
using TeamSamsara.Shared.Alert;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Tests;

public class EmailChangeServiceTests : IAsyncLifetime
{
    #region Fields

    private const string UserId = "user-1";
    private const string OtherUserId = "user-2";
    private const string Email = "jane.doe@example.com";
    private const string NewEmail = "jane.new@example.com";
    private const string AnotherEmail = "jane.other@example.com";
    private const string WrongCode = "not-the-code";
    private const long FirstSignIn = 1_700_000_000;

    private readonly InMemoryUserStore _users = new();
    private readonly InMemoryVerificationCodeStore _codes = new();
    private readonly InMemoryEmailChangeRequestStore _requests = new();
    private readonly InMemoryCacheService _cache = new();
    private readonly RecordingAlertSender _alerts = new();
    private readonly RecordingBackgroundTaskQueue _queue = new();
    private readonly FakeAccountGateway _accounts = new();
    private readonly FakeClock _clock = new();
    private readonly VerificationSettings _settings = new();
    private readonly RecordingLogger<EmailChangeService> _logger = new();
    private readonly SignInTracker _signIns;
    private readonly ServiceProvider _provider;
    private readonly EmailChangeService _service;

    #endregion

    #region Constructors

    public EmailChangeServiceTests()
    {
        var verification = new VerificationCodeService(
            _codes, _alerts, _clock, Options.Create(_settings));

        var delivery = new EmailChangeDelivery(
            _accounts, verification, NullLogger<EmailChangeDelivery>.Instance);

        _signIns = new SignInTracker(_users, _cache, Options.Create(new AuthenticationSettings()));

        _provider = new ServiceCollection()
            .AddSingleton<IEmailChangeDelivery>(delivery)
            .BuildServiceProvider();

        _service = new EmailChangeService(
            _queue,
            verification,
            _requests,
            _users,
            _accounts,
            _signIns,
            _alerts,
            _clock,
            Options.Create(_settings),
            _logger);

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
            VerifiedSignIns = new List<long> { FirstSignIn }
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
        var result = await _service.RequestChangeAsync(UserId, NewEmail);

        result.ShouldBe(ExpectedAnswer());
    }

    [Fact]
    public async Task Request_ForATakenAddress_IsRefused_WithNothingSavedOrQueued()
    {
        _accounts.AddAccount(OtherUserId, AnotherEmail);

        var result = await _service.RequestChangeAsync(UserId, AnotherEmail);

        result.Status.ShouldBe(EmailChangeStatus.EmailTaken);
        _requests.Count.ShouldBe(0);
        _queue.Count.ShouldBe(0);
        _alerts.Sent.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(Email)]
    [InlineData("JANE.DOE@example.com")]
    [InlineData("  jane.doe@example.com ")]
    public async Task Request_ForTheMembersOwnAddress_IsRefused_IgnoringCaseAndSpaces(string address)
    {
        var result = await _service.RequestChangeAsync(UserId, address);

        result.Status.ShouldBe(EmailChangeStatus.SameAsCurrentEmail);
        _requests.Count.ShouldBe(0);
        _queue.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Request_AfterARefusal_StartsNoCooldown()
    {
        _accounts.AddAccount(OtherUserId, AnotherEmail);

        await _service.RequestChangeAsync(UserId, AnotherEmail);
        await _service.RequestChangeAsync(UserId, Email);
        var result = await _service.RequestChangeAsync(UserId, NewEmail);

        result.ShouldBe(ExpectedAnswer());
    }

    [Fact]
    public async Task Request_Accepted_IsLogged_WithTheAddressMasked()
    {
        await _service.RequestChangeAsync(UserId, NewEmail);

        var entry = _logger.Entries.ShouldHaveSingleItem();
        entry.Message.ShouldContain("accepted");
        entry.Message.ShouldContain("j***@example.com");
        entry.Message.ShouldNotContain(NewEmail);
    }

    [Fact]
    public async Task Request_Refused_IsLogged_WithTheReason_AndTheAddressMasked()
    {
        _accounts.AddAccount(OtherUserId, AnotherEmail);

        await _service.RequestChangeAsync(UserId, AnotherEmail);

        var entry = _logger.Entries.ShouldHaveSingleItem();
        entry.Message.ShouldContain(nameof(EmailChangeStatus.EmailTaken));
        entry.Message.ShouldNotContain(AnotherEmail);
    }

    [Fact]
    public async Task Request_WithinTheCooldown_IsLogged()
    {
        await _service.RequestChangeAsync(UserId, NewEmail);

        await _service.RequestChangeAsync(UserId, AnotherEmail);

        _logger.Entries.ShouldContain(entry => entry.Message.Contains("cooldown"));
    }

    [Fact]
    public async Task AWholeEmailChange_NeverLogsAFullAddressOrACode()
    {
        var codes = await RequestAndGetCodesAsync();
        await _service.ConfirmAsync(UserId, codes.Old, codes.New);

        _logger.Entries.ShouldNotBeEmpty();
        _logger.Entries.ShouldAllBe(entry =>
            !entry.Message.Contains(Email)
            && !entry.Message.Contains(NewEmail)
            && !entry.Message.Contains(codes.Old)
            && !entry.Message.Contains(codes.New));
    }

    [Fact]
    public async Task Request_SendsNothingUntilTheQueuedWorkRuns()
    {
        await _service.RequestChangeAsync(UserId, NewEmail);

        _queue.Count.ShouldBe(1);
        _alerts.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task Request_SavesThePendingChange()
    {
        await _service.RequestChangeAsync(UserId, NewEmail);

        var pending = _requests.Find(UserId);
        pending.ShouldNotBeNull();
        pending.NewEmail.ShouldBe(NewEmail);
        pending.CreatedAt.ShouldBe(_clock.UtcNow);
        pending.ExpiresAt.ShouldBe(_clock.UtcNow.AddMinutes(_settings.ExpiryMinutes));
        pending.OldCodeVerified.ShouldBeFalse();
        pending.NewCodeVerified.ShouldBeFalse();
    }

    [Fact]
    public async Task Request_TrimsTheAddress()
    {
        await _service.RequestChangeAsync(UserId, "  " + NewEmail + " ");

        _requests.Find(UserId)!.NewEmail.ShouldBe(NewEmail);
    }

    [Fact]
    public async Task Request_ForAFreeAddress_EmailsBothAddressesWhenTheQueuedWorkRuns()
    {
        await RequestAndDeliverAsync();

        _alerts.Sent.Select(alert => alert.To).ShouldBe(new[] { Email, NewEmail }, ignoreOrder: true);
    }

    [Fact]
    public async Task Request_ForAnAddressTakenBeforeTheQueuedWorkRuns_EmailsOnlyTheCurrentAddress()
    {
        await RequestThenLoseTheAddressAndDeliverAsync();

        _alerts.Sent.ShouldHaveSingleItem().To.ShouldBe(Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("Jane <jane@example.com>")]
    public async Task Request_WithAnInvalidAddress_IsRejected_AndQueuesNothing(string address)
    {
        var result = await _service.RequestChangeAsync(UserId, address);

        result.Status.ShouldBe(EmailChangeStatus.InvalidEmail);
        _queue.Count.ShouldBe(0);
        _requests.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Request_WithAnAddressThatIsTooLong_IsRejected()
    {
        var result = await _service.RequestChangeAsync(UserId, new string('a', 250) + "@example.com");

        result.Status.ShouldBe(EmailChangeStatus.InvalidEmail);
    }

    [Fact]
    public async Task Request_ForAMissingAccount_IsRejected()
    {
        var result = await _service.RequestChangeAsync("stranger", NewEmail);

        result.Status.ShouldBe(EmailChangeStatus.AccountNotFound);
        _queue.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Request_WithinTheCooldown_IsRejected_WithTheRemainingWait()
    {
        await _service.RequestChangeAsync(UserId, NewEmail);
        _clock.Advance(TimeSpan.FromSeconds(20));

        var result = await _service.RequestChangeAsync(UserId, AnotherEmail);

        result.Status.ShouldBe(EmailChangeStatus.CooldownActive);
        result.RetryAfterSeconds.ShouldBe(_settings.ResendCooldownSeconds - 20);
        _requests.Find(UserId)!.NewEmail.ShouldBe(NewEmail);
    }

    [Fact]
    public async Task Request_AfterTheCooldown_ReplacesThePendingChange_AndClearsItsFlags()
    {
        await _service.RequestChangeAsync(UserId, NewEmail);
        await _requests.MarkVerifiedAsync(UserId, VerificationPurpose.EmailChangeOld);
        PassTheCooldown();

        await _service.RequestChangeAsync(UserId, AnotherEmail);

        var pending = _requests.Find(UserId)!;
        pending.NewEmail.ShouldBe(AnotherEmail);
        pending.OldCodeVerified.ShouldBeFalse();
    }

    [Fact]
    public async Task Request_WhenTheQueueIsFull_StillGivesTheSameAnswer()
    {
        _queue.Accepting = false;

        var result = await _service.RequestChangeAsync(UserId, NewEmail);

        result.ShouldBe(ExpectedAnswer());
        _queue.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Confirm_WithBothRightCodes_ChangesTheEmail()
    {
        var result = await ConfirmWithRealCodesAsync();

        result.ShouldBe(EmailChangeStatus.Success);
        (await _accounts.GetEmailAsync(UserId)).ShouldBe(NewEmail);
    }

    [Fact]
    public async Task Confirm_RevokesEverySession()
    {
        await ConfirmWithRealCodesAsync();

        _accounts.RevokedSessions.ShouldHaveSingleItem().ShouldBe(UserId);
    }

    [Fact]
    public async Task Confirm_ForgetsTheVerifiedSignIns()
    {
        (await _signIns.IsVerifiedAsync(UserId, FirstSignIn)).ShouldBeTrue();

        await ConfirmWithRealCodesAsync();

        (await _signIns.IsVerifiedAsync(UserId, FirstSignIn)).ShouldBeFalse();
    }

    [Fact]
    public async Task Confirm_NotifiesTheOldAddress_NamingTheNewOneMasked()
    {
        await ConfirmWithRealCodesAsync();

        var notice = _alerts.Sent.Last();
        notice.Type.ShouldBe(AlertType.EmailChanged);
        notice.To.ShouldBe(Email);
        notice.TemplateData[AlertTemplateKeys.NewEmail].ShouldBe("j***@example.com");
    }

    [Fact]
    public async Task Confirm_RemovesThePendingChange_SoItCannotBeRepeated()
    {
        var oldCode = await RequestAndGetCodesAsync();

        await _service.ConfirmAsync(UserId, oldCode.Old, oldCode.New);
        var second = await _service.ConfirmAsync(UserId, oldCode.Old, oldCode.New);

        second.ShouldBe(EmailChangeStatus.NoPendingRequest);
        _requests.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Confirm_WhenTheNoticeFails_StillSucceeds()
    {
        _alerts.FailingTypes.Add(AlertType.EmailChanged);

        var result = await ConfirmWithRealCodesAsync();

        result.ShouldBe(EmailChangeStatus.Success);
        (await _accounts.GetEmailAsync(UserId)).ShouldBe(NewEmail);
    }

    [Fact]
    public async Task Confirm_WithAWrongNewCode_IsInvalid_AndKeepsTheEmail()
    {
        var codes = await RequestAndGetCodesAsync();

        var result = await _service.ConfirmAsync(UserId, codes.Old, WrongCode);

        result.ShouldBe(EmailChangeStatus.InvalidCode);
        (await _accounts.GetEmailAsync(UserId)).ShouldBe(Email);
    }

    [Fact]
    public async Task Confirm_AWrongOldCodeAndAWrongNewCode_GiveTheSameAnswer()
    {
        var first = await RequestAndGetCodesAsync();
        var wrongOld = await _service.ConfirmAsync(UserId, WrongCode, first.New);

        PassTheCooldown();
        var second = await RequestAndGetCodesAsync();
        var wrongNew = await _service.ConfirmAsync(UserId, second.Old, WrongCode);

        wrongOld.ShouldBe(EmailChangeStatus.InvalidCode);
        wrongNew.ShouldBe(wrongOld);
    }

    [Fact]
    public async Task Confirm_ARightCodeIsRemembered_SoOnlyTheOtherNeedsRetyping()
    {
        var codes = await RequestAndGetCodesAsync();

        var first = await _service.ConfirmAsync(UserId, codes.Old, WrongCode);
        var second = await _service.ConfirmAsync(UserId, WrongCode, codes.New);

        first.ShouldBe(EmailChangeStatus.InvalidCode);
        _requests.Find(UserId).ShouldBeNull();
        second.ShouldBe(EmailChangeStatus.Success);
    }

    [Fact]
    public async Task Confirm_WithOnlyTheOldCode_IsAccepted_AndChangesNothing()
    {
        var codes = await RequestAndGetCodesAsync();

        var result = await _service.ConfirmAsync(UserId, codes.Old, null);

        result.ShouldBe(EmailChangeStatus.CodeAccepted);
        (await _accounts.GetEmailAsync(UserId)).ShouldBe(Email);
        _accounts.RevokedSessions.ShouldBeEmpty();
        _requests.Find(UserId)!.OldCodeVerified.ShouldBeTrue();
        _requests.Find(UserId)!.NewCodeVerified.ShouldBeFalse();
    }

    [Fact]
    public async Task Confirm_WithOnlyTheNewCode_IsAccepted_AndChangesNothing()
    {
        var codes = await RequestAndGetCodesAsync();

        var result = await _service.ConfirmAsync(UserId, null, codes.New);

        result.ShouldBe(EmailChangeStatus.CodeAccepted);
        (await _accounts.GetEmailAsync(UserId)).ShouldBe(Email);
        _accounts.RevokedSessions.ShouldBeEmpty();
        _requests.Find(UserId)!.OldCodeVerified.ShouldBeFalse();
        _requests.Find(UserId)!.NewCodeVerified.ShouldBeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Confirm_WithOneCodeAtATime_ChangesTheEmailOnTheSecond(bool oldFirst)
    {
        var codes = await RequestAndGetCodesAsync();

        var first = oldFirst
            ? await _service.ConfirmAsync(UserId, codes.Old, null)
            : await _service.ConfirmAsync(UserId, null, codes.New);
        var second = oldFirst
            ? await _service.ConfirmAsync(UserId, null, codes.New)
            : await _service.ConfirmAsync(UserId, codes.Old, null);

        first.ShouldBe(EmailChangeStatus.CodeAccepted);
        second.ShouldBe(EmailChangeStatus.Success);
        (await _accounts.GetEmailAsync(UserId)).ShouldBe(NewEmail);
        _accounts.RevokedSessions.ShouldHaveSingleItem().ShouldBe(UserId);
    }

    [Fact]
    public async Task Confirm_AWrongSingleCode_IsInvalid_AndKeepsTheOtherAcceptedCode()
    {
        var codes = await RequestAndGetCodesAsync();

        var accepted = await _service.ConfirmAsync(UserId, codes.Old, null);
        var wrong = await _service.ConfirmAsync(UserId, null, WrongCode);
        var final = await _service.ConfirmAsync(UserId, null, codes.New);

        accepted.ShouldBe(EmailChangeStatus.CodeAccepted);
        wrong.ShouldBe(EmailChangeStatus.InvalidCode);
        final.ShouldBe(EmailChangeStatus.Success);
    }

    [Fact]
    public async Task Confirm_SendingAnAcceptedCodeAgain_StillAnswersCodeAccepted()
    {
        var codes = await RequestAndGetCodesAsync();

        await _service.ConfirmAsync(UserId, codes.Old, null);
        var again = await _service.ConfirmAsync(UserId, codes.Old, null);

        again.ShouldBe(EmailChangeStatus.CodeAccepted);
    }

    [Fact]
    public async Task Confirm_SendingNothingMore_AfterOneCodeWasAccepted_IsInvalid()
    {
        var codes = await RequestAndGetCodesAsync();
        await _service.ConfirmAsync(UserId, codes.Old, null);

        var result = await _service.ConfirmAsync(UserId, " ", null);

        result.ShouldBe(EmailChangeStatus.InvalidCode);
        _requests.Find(UserId)!.OldCodeVerified.ShouldBeTrue();
    }

    [Fact]
    public async Task Confirm_WrongGuessesSpreadOverSingleCodeCalls_StillCancelTheRequest()
    {
        await RequestAndGetCodesAsync();

        for (var attempt = 1; attempt < _settings.MaxFailedAttempts; attempt++)
        {
            var result = await _service.ConfirmAsync(UserId, null, WrongCode);
            result.ShouldBe(EmailChangeStatus.InvalidCode);
        }

        var final = await _service.ConfirmAsync(UserId, null, WrongCode);

        final.ShouldBe(EmailChangeStatus.TooManyAttempts);
        _requests.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Confirm_ForAnAddressTakenAfterTheRequest_TheOldCodeIsAccepted_ButTheNewCodeNeverCompletes()
    {
        await RequestThenLoseTheAddressAndDeliverAsync();

        var accepted = await _service.ConfirmAsync(UserId, CodeSentTo(Email), null);
        var wrong = await _service.ConfirmAsync(UserId, null, WrongCode);

        accepted.ShouldBe(EmailChangeStatus.CodeAccepted);
        wrong.ShouldBe(EmailChangeStatus.InvalidCode);
        (await _accounts.GetEmailAsync(UserId)).ShouldBe(Email);
    }

    [Fact]
    public async Task Confirm_ForAnAddressTakenAfterTheRequest_NeverSucceeds_AndLooksLikeAWrongCode()
    {
        await RequestThenLoseTheAddressAndDeliverAsync();
        var oldCode = CodeSentTo(Email);

        var result = await _service.ConfirmAsync(UserId, oldCode, WrongCode);

        result.ShouldBe(EmailChangeStatus.InvalidCode);
        (await _accounts.GetEmailAsync(UserId)).ShouldBe(Email);
    }

    [Fact]
    public async Task Confirm_IfTheAddressIsTakenMeanwhile_IsInvalid_AndKeepsTheEmail()
    {
        var codes = await RequestAndGetCodesAsync();
        _accounts.AddAccount(OtherUserId, NewEmail);

        var result = await _service.ConfirmAsync(UserId, codes.Old, codes.New);

        result.ShouldBe(EmailChangeStatus.InvalidCode);
        (await _accounts.GetEmailAsync(UserId)).ShouldBe(Email);
        _accounts.RevokedSessions.ShouldBeEmpty();
        _logger.Entries.ShouldContain(entry =>
            entry.Level == LogLevel.Warning && entry.Message.Contains("taken meanwhile"));
    }

    [Fact]
    public async Task Confirm_TooManyWrongGuesses_CancelsTheRequest()
    {
        var codes = await RequestAndGetCodesAsync();

        await GuessWrongUntilBurnedAsync(codes.Old);

        _requests.Count.ShouldBe(0);
        (await _service.ConfirmAsync(UserId, codes.Old, codes.New)).ShouldBe(EmailChangeStatus.NoPendingRequest);
    }

    [Fact]
    public async Task Confirm_TooManyWrongGuesses_OnAnAddressTakenAfterTheRequest_CancelsTheRequestToo()
    {
        await RequestThenLoseTheAddressAndDeliverAsync();

        await GuessWrongUntilBurnedAsync(CodeSentTo(Email));

        _requests.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Confirm_WithBlankCodes_IsInvalid()
    {
        await RequestAndGetCodesAsync();

        var result = await _service.ConfirmAsync(UserId, " ", "");

        result.ShouldBe(EmailChangeStatus.InvalidCode);
    }

    [Fact]
    public async Task Confirm_WithNoRequestMade_IsNoPendingRequest()
    {
        var result = await _service.ConfirmAsync(UserId, "12345678", "12345678");

        result.ShouldBe(EmailChangeStatus.NoPendingRequest);
    }

    [Fact]
    public async Task Confirm_AnExpiredRequest_IsNoPendingRequest_AndIsRemoved()
    {
        var codes = await RequestAndGetCodesAsync();
        _clock.Advance(TimeSpan.FromMinutes(_settings.ExpiryMinutes));

        var result = await _service.ConfirmAsync(UserId, codes.Old, codes.New);

        result.ShouldBe(EmailChangeStatus.NoPendingRequest);
        _requests.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Confirm_ForAMissingAccount_IsAccountNotFound()
    {
        var result = await _service.ConfirmAsync("stranger", "12345678", "12345678");

        result.ShouldBe(EmailChangeStatus.AccountNotFound);
    }

    #endregion

    #region Private Methods

    // The fixed answer returned for every accepted request.
    private EmailChangeCodeResult ExpectedAnswer()
    {
        return new EmailChangeCodeResult(
            EmailChangeStatus.Success, _settings.EmailChangeCodeLength, _settings.ResendCooldownSeconds);
    }

    // Moves past the resend cooldown so another request is allowed.
    private void PassTheCooldown()
    {
        _clock.Advance(TimeSpan.FromSeconds(_settings.ResendCooldownSeconds + 1));
    }

    // Requests a change to the new address and runs the queued delivery.
    private async Task RequestAndDeliverAsync()
    {
        await _service.RequestChangeAsync(UserId, NewEmail);
        await _queue.RunAllAsync(_provider);
    }

    // Requests a change, lets another account take the new address before the queued work runs,
    // then runs it. This is the only way left to reach the decoy, since a taken address is refused up front.
    private async Task RequestThenLoseTheAddressAndDeliverAsync()
    {
        await _service.RequestChangeAsync(UserId, NewEmail);
        _accounts.AddAccount(OtherUserId, NewEmail);
        await _queue.RunAllAsync(_provider);
    }

    // The code most recently emailed to an address.
    private string CodeSentTo(string email)
    {
        return _alerts.Sent
            .Last(alert => alert.To == email && alert.TemplateData.ContainsKey(AlertTemplateKeys.Code))
            .TemplateData[AlertTemplateKeys.Code];
    }

    // Requests a change and returns the two codes that were emailed.
    private async Task<(string Old, string New)> RequestAndGetCodesAsync()
    {
        await RequestAndDeliverAsync();

        return (CodeSentTo(Email), CodeSentTo(NewEmail));
    }

    // Requests a change and confirms it with the real codes.
    private async Task<EmailChangeStatus> ConfirmWithRealCodesAsync()
    {
        var codes = await RequestAndGetCodesAsync();

        return await _service.ConfirmAsync(UserId, codes.Old, codes.New);
    }

    // Guesses the new-address code wrong until the code is burned, checking each answer on the way.
    private async Task GuessWrongUntilBurnedAsync(string oldCode)
    {
        for (var attempt = 1; attempt < _settings.MaxFailedAttempts; attempt++)
        {
            var result = await _service.ConfirmAsync(UserId, oldCode, WrongCode);
            result.ShouldBe(EmailChangeStatus.InvalidCode);
        }

        var final = await _service.ConfirmAsync(UserId, oldCode, WrongCode);

        final.ShouldBe(EmailChangeStatus.TooManyAttempts);
    }

    #endregion
}
