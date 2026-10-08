// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/RegistrationServiceTests.cs
// Version : 1.1.0
// Latest commit: fix/neutral-default-display-name
// Author : Gerrah
// Purpose : Proves registration: a new account becomes a Guest and gets a code; the right code
// makes a Member with a Profile; wrong or missing codes change nothing; and a failure partway
// leaves a Guest who can safely try again.

using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Tests.Fakes;
using TeamSamsara.Shared.Alert;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Tests;

public class RegistrationServiceTests
{
    #region Fields

    private const string UserId = "user-1";
    private const string Email = "jane.doe@example.com";
    private const string EmailLocalPart = "jane.doe";
    private const string NeutralDisplayName = "Member";

    private readonly InMemoryUserStore _users = new();
    private readonly InMemoryProfileStore _profiles = new();
    private readonly InMemoryVerificationCodeStore _codes = new();
    private readonly RecordingAlertSender _alerts = new();
    private readonly FakeAccountGateway _accounts = new();
    private readonly FakeGuardDog _guardDog = new();
    private readonly FakeClock _clock = new();
    private readonly VerificationSettings _verificationSettings = new();
    private readonly RegistrationService _service;

    #endregion

    #region Constructors

    public RegistrationServiceTests()
    {
        var verification = new VerificationCodeService(
            _codes, _alerts, _clock, Options.Create(_verificationSettings));

        _service = new RegistrationService(
            _users, _profiles, _accounts, verification, _guardDog, _clock);

        _accounts.AddAccount(UserId, Email);
    }

    #endregion

    #region Public Methods

    [Fact]
    public async Task Start_ForANewAccount_RecordsAGuest_AndSendsTheCode()
    {
        var result = await _service.StartAsync(UserId);

        result.Status.ShouldBe(RegistrationStatus.Success);
        result.CodeLength.ShouldBe(_verificationSettings.RegistrationCodeLength);

        var user = await _users.GetByIdAsync(UserId);
        user.ShouldNotBeNull();
        user.AccessLevel.ShouldBe(AccessLevel.Guest);

        var alert = _alerts.Sent.ShouldHaveSingleItem();
        alert.To.ShouldBe(Email);
        alert.Type.ShouldBe(AlertType.RegistrationCode);
    }

    [Fact]
    public async Task Start_ForAnUnknownAccount_ReportsAccountNotFound_AndRecordsNothing()
    {
        var result = await _service.StartAsync("nobody");

        result.Status.ShouldBe(RegistrationStatus.AccountNotFound);
        (await _users.GetByIdAsync("nobody")).ShouldBeNull();
        _alerts.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task Start_ForAnExistingMember_ReportsAlreadyRegistered()
    {
        _users.Seed(NewUser(AccessLevel.Member));

        var result = await _service.StartAsync(UserId);

        result.Status.ShouldBe(RegistrationStatus.AlreadyRegistered);
        _alerts.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task Start_Twice_ReportsCooldown_AndSendsOnlyOneEmail()
    {
        await _service.StartAsync(UserId);

        var second = await _service.StartAsync(UserId);

        second.Status.ShouldBe(RegistrationStatus.CooldownActive);
        second.RetryAfterSeconds.ShouldBeGreaterThan(0);
        _alerts.Sent.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Resend_BeforeStarting_ReportsAccountNotFound()
    {
        var result = await _service.ResendAsync(UserId);

        result.Status.ShouldBe(RegistrationStatus.AccountNotFound);
    }

    [Fact]
    public async Task Resend_AfterTheCooldown_SendsANewCode()
    {
        await _service.StartAsync(UserId);
        _clock.Advance(TimeSpan.FromSeconds(_verificationSettings.ResendCooldownSeconds + 1));

        var result = await _service.ResendAsync(UserId);

        result.Status.ShouldBe(RegistrationStatus.Success);
        _alerts.Sent.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Confirm_WithTheRightCode_MakesAMember_WithAProfile()
    {
        await _service.StartAsync(UserId);

        var status = await _service.ConfirmAsync(UserId, _alerts.LastCode);

        status.ShouldBe(RegistrationStatus.Success);

        var user = await _users.GetByIdAsync(UserId);
        user.ShouldNotBeNull();
        user.AccessLevel.ShouldBe(AccessLevel.Member);
        user.KnownFingerprints.ShouldContain(known => known.Hash == _guardDog.Current.Fingerprint);

        _accounts.AccessLevels[UserId].ShouldBe(AccessLevel.Member);

        var profile = await _profiles.GetByIdAsync(UserId);
        profile.ShouldNotBeNull();
        profile.DisplayName.ShouldBe(NeutralDisplayName);
    }

    [Fact]
    public async Task Confirm_NeverPutsAnyPartOfTheEmailInTheProfile()
    {
        await _service.StartAsync(UserId);

        await _service.ConfirmAsync(UserId, _alerts.LastCode);

        var profile = (await _profiles.GetByIdAsync(UserId)).ShouldNotBeNull();
        profile.DisplayName.ShouldNotContain(EmailLocalPart, Case.Insensitive);
    }

    [Fact]
    public async Task Confirm_WithAWrongCode_ChangesNothing()
    {
        await _service.StartAsync(UserId);

        var status = await _service.ConfirmAsync(UserId, "not-the-code");

        status.ShouldBe(RegistrationStatus.InvalidCode);
        (await _users.GetByIdAsync(UserId))!.AccessLevel.ShouldBe(AccessLevel.Guest);
        _accounts.AccessLevels.ShouldBeEmpty();
        (await _profiles.GetByIdAsync(UserId)).ShouldBeNull();
    }

    [Fact]
    public async Task Confirm_BeforeStarting_ReportsAccountNotFound()
    {
        var status = await _service.ConfirmAsync(UserId, "123456");

        status.ShouldBe(RegistrationStatus.AccountNotFound);
    }

    [Fact]
    public async Task Confirm_ForAnExistingMember_ReportsAlreadyRegistered()
    {
        _users.Seed(NewUser(AccessLevel.Member));

        var status = await _service.ConfirmAsync(UserId, "123456");

        status.ShouldBe(RegistrationStatus.AlreadyRegistered);
    }

    [Fact]
    public async Task Confirm_KeepsAProfileThatAlreadyExists()
    {
        await _profiles.CreateAsync(new Profile { Id = UserId, DisplayName = "Existing" });
        await _service.StartAsync(UserId);

        await _service.ConfirmAsync(UserId, _alerts.LastCode);

        (await _profiles.GetByIdAsync(UserId))!.DisplayName.ShouldBe("Existing");
    }

    [Fact]
    public async Task Confirm_WhenGrantingMemberFails_LeavesAGuestWhoCanTryAgain()
    {
        await _service.StartAsync(UserId);
        _accounts.SetAccessLevelShouldFail = true;

        await Should.ThrowAsync<InvalidOperationException>(
            () => _service.ConfirmAsync(UserId, _alerts.LastCode));

        (await _users.GetByIdAsync(UserId))!.AccessLevel.ShouldBe(AccessLevel.Guest);

        _accounts.SetAccessLevelShouldFail = false;
        _clock.Advance(TimeSpan.FromSeconds(_verificationSettings.ResendCooldownSeconds + 1));
        await _service.ResendAsync(UserId);
        var status = await _service.ConfirmAsync(UserId, _alerts.LastCode);

        status.ShouldBe(RegistrationStatus.Success);
        (await _users.GetByIdAsync(UserId))!.AccessLevel.ShouldBe(AccessLevel.Member);
        (await _profiles.GetByIdAsync(UserId)).ShouldNotBeNull();
    }

    #endregion

    #region Private Methods

    private User NewUser(AccessLevel accessLevel)
    {
        return new User { Id = UserId, AccessLevel = accessLevel, CreatedAt = _clock.UtcNow };
    }

    #endregion
}
