// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/AccountDeletionServiceTests.cs
// Version : 1.0.0
// Latest commit: feat/account-deletion
// Author : Gerrah
// Purpose : Proves account deletion: only the right code deletes, once, and the member stops counting as Member at once even with warm caches; revocation or notice failures cannot undo it.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Repositories;
using TeamSamsara.Modules.Identity.Tests.Fakes;
using TeamSamsara.Shared.Alert;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Tests;

public class AccountDeletionServiceTests
{
    #region Fields

    private const string UserId = "user-1";
    private const string Email = "jane.doe@example.com";
    private const string WrongCode = "not-the-code";
    private const long FirstSignIn = 1_700_000_000;
    private const long SecondSignIn = 1_700_000_100;

    private readonly InMemoryUserStore _users = new();
    private readonly InMemoryVerificationCodeStore _codes = new();
    private readonly InMemoryCacheService _cache = new();
    private readonly RecordingAlertSender _alerts = new();
    private readonly FakeAccountGateway _accounts = new();
    private readonly FakeClock _clock = new();
    private readonly VerificationSettings _verificationSettings = new();
    private readonly AuthenticationSettings _authenticationSettings = new();
    private readonly SignInTracker _signIns;
    private readonly VerificationCodeService _verification;
    private readonly AccountDeletionService _service;

    #endregion

    #region Constructors

    public AccountDeletionServiceTests()
    {
        _verification = new VerificationCodeService(
            _codes, _alerts, _clock, Options.Create(_verificationSettings));

        _signIns = new SignInTracker(_users, _cache, Options.Create(_authenticationSettings));
        _service = CreateService(_users);

        _accounts.AddAccount(UserId, Email);
        _users.Seed(new User
        {
            Id = UserId,
            AccessLevel = AccessLevel.Member,
            CreatedAt = _clock.UtcNow,
            VerifiedSignIns = new List<long> { FirstSignIn, SecondSignIn }
        });
    }

    #endregion

    #region Public Methods

    [Fact]
    public async Task RequestCode_SendsAStepUpCodeToTheAccountEmail()
    {
        var result = await _service.RequestDeletionCodeAsync(UserId);

        result.Status.ShouldBe(AccountDeletionStatus.Success);
        result.CodeLength.ShouldBe(_verificationSettings.StepUpCodeLength);

        var alert = _alerts.Sent.ShouldHaveSingleItem();
        alert.To.ShouldBe(Email);
        alert.Type.ShouldBe(AlertType.StepUpCode);
    }

    [Fact]
    public async Task RequestCode_AgainTooSoon_ReportsCooldown_AndSendsNothingMore()
    {
        await _service.RequestDeletionCodeAsync(UserId);

        var result = await _service.RequestDeletionCodeAsync(UserId);

        result.Status.ShouldBe(AccountDeletionStatus.CooldownActive);
        result.RetryAfterSeconds.ShouldBeGreaterThan(0);
        _alerts.Sent.Count.ShouldBe(1);
    }

    [Fact]
    public async Task RequestCode_ForAnUnknownAccount_IsNotFound()
    {
        var result = await _service.RequestDeletionCodeAsync("ghost");

        result.Status.ShouldBe(AccountDeletionStatus.AccountNotFound);
        _alerts.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task RequestCode_ForAGuest_IsNotFound()
    {
        SeedGuest();

        var result = await _service.RequestDeletionCodeAsync("guest-1");

        result.Status.ShouldBe(AccountDeletionStatus.AccountNotFound);
        _alerts.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task RequestCode_ForAnAlreadyDeletedAccount_IsNotFound()
    {
        var code = await RequestCodeAsync();
        await _service.DeleteAsync(UserId, code);
        _alerts.Sent.Clear();

        var result = await _service.RequestDeletionCodeAsync(UserId);

        result.Status.ShouldBe(AccountDeletionStatus.AccountNotFound);
        _alerts.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task Delete_WithTheRightCode_MarksTheAccountDeleted()
    {
        var code = await RequestCodeAsync();

        var status = await _service.DeleteAsync(UserId, code);

        status.ShouldBe(AccountDeletionStatus.Success);

        var stored = await _users.GetByIdAsync(UserId);
        stored.ShouldNotBeNull();
        stored.DeletedAt.ShouldBe(_clock.UtcNow);
    }

    [Fact]
    public async Task Delete_WithTheRightCode_StopsTheSignInsCountingAtOnce_EvenWithAWarmCache()
    {
        var code = await RequestCodeAsync();
        (await _signIns.IsVerifiedAsync(UserId, FirstSignIn)).ShouldBeTrue();

        await _service.DeleteAsync(UserId, code);

        (await _signIns.IsVerifiedAsync(UserId, FirstSignIn)).ShouldBeFalse();
        (await _signIns.IsVerifiedAsync(UserId, SecondSignIn)).ShouldBeFalse();

        var stored = await _users.GetByIdAsync(UserId);
        stored.ShouldNotBeNull();
        stored.VerifiedSignIns.ShouldBeEmpty();
    }

    [Fact]
    public async Task Delete_WithTheRightCode_RevokesTheRefreshTokens()
    {
        var code = await RequestCodeAsync();

        await _service.DeleteAsync(UserId, code);

        _accounts.RevokedSessions.ShouldHaveSingleItem().ShouldBe(UserId);
    }

    [Fact]
    public async Task Delete_WithTheRightCode_SendsTheNoticeWithTheRecoveryWindow()
    {
        var code = await RequestCodeAsync();

        await _service.DeleteAsync(UserId, code);

        var notice = _alerts.Sent.Last();
        notice.Type.ShouldBe(AlertType.AccountDeleted);
        notice.To.ShouldBe(Email);
        notice.TemplateData[AlertTemplateKeys.RecoveryDays]
            .ShouldBe(_authenticationSettings.RecoveryWindowDays.ToString());
    }

    [Fact]
    public async Task Delete_WithAWrongCode_ChangesNothing()
    {
        await RequestCodeAsync();

        var status = await _service.DeleteAsync(UserId, WrongCode);

        status.ShouldBe(AccountDeletionStatus.InvalidCode);
        await AssertUntouchedAsync();
    }

    [Fact]
    public async Task Delete_WithAnExpiredCode_ChangesNothing()
    {
        var code = await RequestCodeAsync();
        _clock.Advance(TimeSpan.FromMinutes(_verificationSettings.ExpiryMinutes));

        var status = await _service.DeleteAsync(UserId, code);

        status.ShouldBe(AccountDeletionStatus.CodeExpired);
        await AssertUntouchedAsync();
    }

    [Fact]
    public async Task Delete_WithoutRequestingACode_ChangesNothing()
    {
        var status = await _service.DeleteAsync(UserId, "12345678");

        status.ShouldBe(AccountDeletionStatus.NoPendingCode);
        await AssertUntouchedAsync();
    }

    [Fact]
    public async Task Delete_AfterTooManyWrongGuesses_BurnsTheCode_EvenIfTheRightOneFollows()
    {
        var code = await RequestCodeAsync();
        var last = AccountDeletionStatus.Success;

        for (var guess = 0; guess < _verificationSettings.MaxFailedAttempts; guess++)
        {
            last = await _service.DeleteAsync(UserId, WrongCode);
        }

        var afterwards = await _service.DeleteAsync(UserId, code);

        last.ShouldBe(AccountDeletionStatus.TooManyAttempts);
        afterwards.ShouldBe(AccountDeletionStatus.NoPendingCode);
        await AssertUntouchedAsync();
    }

    [Fact]
    public async Task Delete_ForAnUnknownAccount_IsNotFound()
    {
        var status = await _service.DeleteAsync("ghost", "12345678");

        status.ShouldBe(AccountDeletionStatus.AccountNotFound);
    }

    [Fact]
    public async Task Delete_ForAGuest_IsNotFound()
    {
        SeedGuest();

        var status = await _service.DeleteAsync("guest-1", "12345678");

        status.ShouldBe(AccountDeletionStatus.AccountNotFound);
    }

    [Fact]
    public async Task Delete_TheCodeCannotBeUsedAgain_EvenAfterTheAccountIsRestored()
    {
        var code = await RequestCodeAsync();
        await _service.DeleteAsync(UserId, code);
        await _users.ModifyAsync(UserId, user => user.DeletedAt = null);

        var status = await _service.DeleteAsync(UserId, code);

        status.ShouldBe(AccountDeletionStatus.NoPendingCode);

        var stored = await _users.GetByIdAsync(UserId);
        stored.ShouldNotBeNull();
        stored.DeletedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Delete_WhenARivalRequestDeletesFirst_LosesTheRace_AndChangesNothingMore()
    {
        var rivalTime = _clock.UtcNow.AddMinutes(-1);
        var racing = new FaultyUserStore(_users) { RivalDeletedAt = rivalTime };
        var service = CreateService(racing);
        var code = await RequestCodeAsync();

        var status = await service.DeleteAsync(UserId, code);

        status.ShouldBe(AccountDeletionStatus.NoPendingCode);
        _accounts.RevokedSessions.ShouldBeEmpty();
        _alerts.Sent.Any(alert => alert.Type == AlertType.AccountDeleted).ShouldBeFalse();

        var stored = await _users.GetByIdAsync(UserId);
        stored.ShouldNotBeNull();
        stored.DeletedAt.ShouldBe(rivalTime);
    }

    [Fact]
    public async Task Delete_WhenTheCommitFails_ChangesNothing_AndSendsNoNotice()
    {
        var failing = new FaultyUserStore(_users) { ThrowOnModify = true };
        var service = CreateService(failing);
        var code = await RequestCodeAsync();

        await Should.ThrowAsync<InvalidOperationException>(() => service.DeleteAsync(UserId, code));

        await AssertUntouchedAsync();
    }

    [Fact]
    public async Task Delete_WhenRevokingTheSessionsFails_IsStillDeleted_AndBlockedAtOnce()
    {
        var code = await RequestCodeAsync();
        _accounts.RevokeSessionsShouldFail = true;

        var status = await _service.DeleteAsync(UserId, code);

        status.ShouldBe(AccountDeletionStatus.Success);
        (await _signIns.IsVerifiedAsync(UserId, FirstSignIn)).ShouldBeFalse();

        var stored = await _users.GetByIdAsync(UserId);
        stored.ShouldNotBeNull();
        stored.DeletedAt.ShouldNotBeNull();
        _alerts.Sent.Last().Type.ShouldBe(AlertType.AccountDeleted);
    }

    [Fact]
    public async Task Delete_WhenTheNoticeFails_IsStillDeleted()
    {
        var code = await RequestCodeAsync();
        _alerts.FailingTypes.Add(AlertType.AccountDeleted);

        var status = await _service.DeleteAsync(UserId, code);

        status.ShouldBe(AccountDeletionStatus.Success);

        var stored = await _users.GetByIdAsync(UserId);
        stored.ShouldNotBeNull();
        stored.DeletedAt.ShouldNotBeNull();
        _accounts.RevokedSessions.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Restore_DoesNotBringBackTheSignInsThatExistedBeforeTheDeletion()
    {
        var code = await RequestCodeAsync();
        await _service.DeleteAsync(UserId, code);

        await _users.ModifyAsync(UserId, user => user.DeletedAt = null);
        await _cache.RemoveAsync(SignInCacheKey());

        (await _signIns.IsVerifiedAsync(UserId, FirstSignIn)).ShouldBeFalse();
        (await _signIns.IsVerifiedAsync(UserId, SecondSignIn)).ShouldBeFalse();
    }

    #endregion

    #region Private Methods

    // Builds the service over the given user store; everything else is shared with the test
    private AccountDeletionService CreateService(IUserStore users)
    {
        return new AccountDeletionService(
            users,
            _accounts,
            _signIns,
            _verification,
            _alerts,
            _clock,
            Options.Create(_authenticationSettings),
            NullLogger<AccountDeletionService>.Instance);
    }

    // Requests a deletion code and returns the code that was emailed
    private async Task<string> RequestCodeAsync()
    {
        var result = await _service.RequestDeletionCodeAsync(UserId);
        result.Status.ShouldBe(AccountDeletionStatus.Success);

        return _alerts.LastCode;
    }

    // Adds a registered-but-incomplete account
    private void SeedGuest()
    {
        _accounts.AddAccount("guest-1", "guest@example.com");
        _users.Seed(new User
        {
            Id = "guest-1",
            AccessLevel = AccessLevel.Guest,
            CreatedAt = _clock.UtcNow
        });
    }

    // Asserts the member is exactly as seeded: not deleted, sign-ins intact, nothing revoked or announced
    private async Task AssertUntouchedAsync()
    {
        var stored = await _users.GetByIdAsync(UserId);
        stored.ShouldNotBeNull();
        stored.DeletedAt.ShouldBeNull();
        stored.VerifiedSignIns.ShouldBe(new List<long> { FirstSignIn, SecondSignIn });
        _accounts.RevokedSessions.ShouldBeEmpty();
        _alerts.Sent.Any(alert => alert.Type == AlertType.AccountDeleted).ShouldBeFalse();
    }

    // The cache key SignInTracker uses for this member
    private static string SignInCacheKey() => "identity:verified-sign-ins:" + UserId;

    #endregion

    #region Nested Types

    // A user store that can throw on modify, or let a rival request delete the account just before the modify
    private sealed class FaultyUserStore : IUserStore
    {
        private readonly IUserStore _inner;

        public FaultyUserStore(IUserStore inner)
        {
            _inner = inner;
        }

        public bool ThrowOnModify { get; set; }

        public DateTimeOffset? RivalDeletedAt { get; set; }

        public Task<User?> GetByIdAsync(string id) => _inner.GetByIdAsync(id);

        public Task CreateAsync(User user) => _inner.CreateAsync(user);

        public Task UpdateAsync(User user) => _inner.UpdateAsync(user);

        public async Task ModifyAsync(string id, Action<User> modify)
        {
            if (ThrowOnModify)
            {
                throw new InvalidOperationException("Simulated store failure.");
            }

            if (RivalDeletedAt is { } rivalDeletedAt)
            {
                await _inner.ModifyAsync(id, user => user.DeletedAt = rivalDeletedAt);
            }

            await _inner.ModifyAsync(id, modify);
        }

        public Task<IReadOnlyList<User>> ListDeletedBeforeAsync(DateTimeOffset cutoff) =>
            _inner.ListDeletedBeforeAsync(cutoff);

        public Task DeleteAsync(string id) => _inner.DeleteAsync(id);
    }

    #endregion
}
