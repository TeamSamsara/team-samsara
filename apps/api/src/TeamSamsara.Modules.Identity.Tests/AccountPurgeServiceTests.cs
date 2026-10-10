// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/AccountPurgeServiceTests.cs
// Version : 1.0.0
// Latest commit: feat/account-purge
// Author : Gerrah
// Purpose : Proves the purge removes only accounts past the recovery window, removes everything they own, and survives failures, leases, restores and repeated runs without losing track of an account.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Repositories;
using TeamSamsara.Modules.Identity.Tests.Fakes;
using TeamSamsara.Shared.Assets;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Tests;

public class AccountPurgeServiceTests
{
    #region Fields

    private const string UserId = "user-1";
    private const string OtherUserId = "user-2";

    private readonly InMemoryUserStore _users = new();
    private readonly InMemoryProfileStore _profiles = new();
    private readonly InMemoryVerificationCodeStore _codes = new();
    private readonly InMemoryPasswordResetTokenStore _resetTokens = new();
    private readonly InMemoryEmailChangeRequestStore _emailChanges = new();
    private readonly FakeAccountGateway _accounts = new();
    private readonly FakeMemberImageService _images = new();
    private readonly FakeClock _clock = new();
    private readonly AuthenticationSettings _authenticationSettings = new();
    private readonly AccountPurgeSettings _purgeSettings = new();
    private readonly AccountPurgeService _service;

    #endregion

    #region Constructors

    public AccountPurgeServiceTests()
    {
        _service = CreateService(_users);
    }

    #endregion

    #region Public Methods

    [Fact]
    public async Task AnExpiredAccount_IsRemovedCompletely()
    {
        var assets = await SeedDeletedAccountAsync(UserId, DaysAgo(31));

        var result = await _service.PurgeExpiredAsync();

        result.ShouldBe(new AccountPurgeResult(1, 0, 0));
        (await _users.GetByIdAsync(UserId)).ShouldBeNull();
        (await _profiles.GetByIdAsync(UserId)).ShouldBeNull();
        _accounts.DeletedAccounts.ShouldBe(new[] { UserId });
        _images.DeletedAssetIds.ShouldBe(assets, ignoreOrder: true);
        await AssertNoLeftoversAsync(UserId);
    }

    [Fact]
    public async Task AnAccountInsideTheWindow_IsLeftAlone()
    {
        await SeedDeletedAccountAsync(UserId, DaysAgo(29));

        var result = await _service.PurgeExpiredAsync();

        result.ShouldBe(new AccountPurgeResult(0, 0, 0));
        (await _users.GetByIdAsync(UserId)).ShouldNotBeNull();
        _accounts.DeletedAccounts.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnAccountDeletedExactlyOneWindowAgo_IsKept_UntilTheWindowHasPassed()
    {
        await SeedDeletedAccountAsync(UserId, DaysAgo(_authenticationSettings.RecoveryWindowDays));

        var atTheBoundary = await _service.PurgeExpiredAsync();
        _clock.Advance(TimeSpan.FromSeconds(1));
        var justAfter = await _service.PurgeExpiredAsync();

        atTheBoundary.ShouldBe(new AccountPurgeResult(0, 0, 0));
        justAfter.ShouldBe(new AccountPurgeResult(1, 0, 0));
        (await _users.GetByIdAsync(UserId)).ShouldBeNull();
    }

    [Fact]
    public async Task AnActiveAccount_IsNeverPurged()
    {
        await SeedActiveAccountAsync(UserId);

        var result = await _service.PurgeExpiredAsync();

        result.ShouldBe(new AccountPurgeResult(0, 0, 0));
        (await _users.GetByIdAsync(UserId)).ShouldNotBeNull();
        _accounts.DeletedAccounts.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnExpiredAccountWithoutAProfile_IsStillRemoved()
    {
        _accounts.AddAccount(UserId, "jane.doe@example.com");
        _users.Seed(NewUser(UserId, DaysAgo(31)));

        var result = await _service.PurgeExpiredAsync();

        result.ShouldBe(new AccountPurgeResult(1, 0, 0));
        (await _users.GetByIdAsync(UserId)).ShouldBeNull();
        _accounts.DeletedAccounts.ShouldBe(new[] { UserId });
    }

    [Fact]
    public async Task PurgingOneAccount_DoesNotTouchAnotherAccountsData()
    {
        await SeedDeletedAccountAsync(UserId, DaysAgo(31));
        await SeedActiveAccountAsync(OtherUserId);

        await _service.PurgeExpiredAsync();

        (await _users.GetByIdAsync(OtherUserId)).ShouldNotBeNull();
        (await _profiles.GetByIdAsync(OtherUserId)).ShouldNotBeNull();
        _resetTokens.Find("token-" + OtherUserId).ShouldNotBeNull();
        _emailChanges.Find(OtherUserId).ShouldNotBeNull();
        (await _codes.GetAsync(OtherUserId, VerificationPurpose.StepUp)).ShouldNotBeNull();
    }

    [Fact]
    public async Task WhenAnImageCannotBeDeleted_TheAccountFails_StaysListed_AndALaterRunFinishesIt()
    {
        await SeedDeletedAccountAsync(UserId, DaysAgo(31));
        _images.FailDeletes = true;

        var failed = await _service.PurgeExpiredAsync();

        failed.ShouldBe(new AccountPurgeResult(0, 0, 1));
        (await _users.GetByIdAsync(UserId)).ShouldNotBeNull();
        (await _profiles.GetByIdAsync(UserId)).ShouldNotBeNull();
        _accounts.DeletedAccounts.ShouldBeEmpty();

        _images.FailDeletes = false;
        PassTheLease();
        var retried = await _service.PurgeExpiredAsync();

        retried.ShouldBe(new AccountPurgeResult(1, 0, 0));
        (await _users.GetByIdAsync(UserId)).ShouldBeNull();
        (await _profiles.GetByIdAsync(UserId)).ShouldBeNull();
    }

    [Fact]
    public async Task WhenTheGatewayCannotDeleteTheAccount_APreviousPartialRunIsFinishedByTheNext()
    {
        await SeedDeletedAccountAsync(UserId, DaysAgo(31));
        _accounts.DeleteShouldFail = true;

        var failed = await _service.PurgeExpiredAsync();

        failed.ShouldBe(new AccountPurgeResult(0, 0, 1));
        (await _profiles.GetByIdAsync(UserId)).ShouldBeNull();
        await AssertNoLeftoversAsync(UserId);
        (await _users.GetByIdAsync(UserId)).ShouldNotBeNull();

        _accounts.DeleteShouldFail = false;
        PassTheLease();
        var retried = await _service.PurgeExpiredAsync();

        retried.ShouldBe(new AccountPurgeResult(1, 0, 0));
        (await _users.GetByIdAsync(UserId)).ShouldBeNull();
        _accounts.DeletedAccounts.ShouldBe(new[] { UserId });
    }

    [Fact]
    public async Task OneFailingAccount_DoesNotStopTheOthers()
    {
        await SeedDeletedAccountAsync(UserId, DaysAgo(31));
        _accounts.AddAccount(OtherUserId, "john.doe@example.com");
        _users.Seed(NewUser(OtherUserId, DaysAgo(31)));
        _images.FailDeletes = true;

        var result = await _service.PurgeExpiredAsync();

        result.ShouldBe(new AccountPurgeResult(1, 0, 1));
        (await _users.GetByIdAsync(UserId)).ShouldNotBeNull();
        (await _users.GetByIdAsync(OtherUserId)).ShouldBeNull();
    }

    [Fact]
    public async Task WhileAnotherInstanceHoldsTheLease_TheAccountIsSkipped()
    {
        await SeedDeletedAccountAsync(UserId, DaysAgo(31));
        await HoldTheLeaseAsync(UserId);

        var result = await _service.PurgeExpiredAsync();

        result.ShouldBe(new AccountPurgeResult(0, 1, 0));
        (await _users.GetByIdAsync(UserId)).ShouldNotBeNull();
        _accounts.DeletedAccounts.ShouldBeEmpty();
    }

    [Fact]
    public async Task OnceTheLeaseHasLapsed_TheAccountIsPurged()
    {
        await SeedDeletedAccountAsync(UserId, DaysAgo(31));
        await HoldTheLeaseAsync(UserId);
        PassTheLease();

        var result = await _service.PurgeExpiredAsync();

        result.ShouldBe(new AccountPurgeResult(1, 0, 0));
        (await _users.GetByIdAsync(UserId)).ShouldBeNull();
    }

    [Fact]
    public async Task AnAccountRestoredAfterBeingListed_IsSkipped_AndKeptWhole()
    {
        await SeedDeletedAccountAsync(UserId, DaysAgo(31));
        var service = CreateService(new RestoringBeforeClaimUserStore(_users));

        var result = await service.PurgeExpiredAsync();

        result.ShouldBe(new AccountPurgeResult(0, 1, 0));
        var stored = await _users.GetByIdAsync(UserId);
        stored.ShouldNotBeNull();
        stored.DeletedAt.ShouldBeNull();
        (await _profiles.GetByIdAsync(UserId)).ShouldNotBeNull();
        _accounts.DeletedAccounts.ShouldBeEmpty();
        _images.DeletedAssetIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunningTwice_FindsNothingTheSecondTime()
    {
        await SeedDeletedAccountAsync(UserId, DaysAgo(31));

        await _service.PurgeExpiredAsync();
        var second = await _service.PurgeExpiredAsync();

        second.ShouldBe(new AccountPurgeResult(0, 0, 0));
        _accounts.DeletedAccounts.ShouldBe(new[] { UserId });
    }

    [Fact]
    public async Task ACancelledRun_StopsInsteadOfPurging()
    {
        await SeedDeletedAccountAsync(UserId, DaysAgo(31));
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => _service.PurgeExpiredAsync(source.Token));

        (await _users.GetByIdAsync(UserId)).ShouldNotBeNull();
    }

    #endregion

    #region Private Methods

    // Builds the service over the given user store; everything else is shared with the test
    private AccountPurgeService CreateService(IUserStore users)
    {
        return new AccountPurgeService(
            users,
            _profiles,
            _codes,
            _resetTokens,
            _emailChanges,
            _accounts,
            _images,
            _clock,
            Options.Create(_authenticationSettings),
            Options.Create(_purgeSettings),
            NullLogger<AccountPurgeService>.Instance);
    }

    // A moment this many days before the test clock's now
    private DateTimeOffset DaysAgo(int days) => _clock.UtcNow.AddDays(-days);

    // Moves the clock past the lease a purge or claim holds
    private void PassTheLease() => _clock.Advance(TimeSpan.FromMinutes(_purgeSettings.LeaseMinutes + 1));

    // Takes the lease the way another instance would
    private async Task HoldTheLeaseAsync(string userId)
    {
        var cutoff = _clock.UtcNow.AddDays(-_authenticationSettings.RecoveryWindowDays);
        var leaseUntil = _clock.UtcNow.AddMinutes(_purgeSettings.LeaseMinutes);

        (await _users.TryClaimForPurgeAsync(userId, cutoff, _clock.UtcNow, leaseUntil)).ShouldBeTrue();
    }

    // A member user, deleted at the given moment unless it is null
    private User NewUser(string userId, DateTimeOffset? deletedAt)
    {
        return new User
        {
            Id = userId,
            AccessLevel = AccessLevel.Member,
            CreatedAt = _clock.UtcNow.AddYears(-1),
            DeletedAt = deletedAt
        };
    }

    // A deleted account with a profile, two images, codes, a reset token and an email change request; returns its asset ids
    private async Task<List<string>> SeedDeletedAccountAsync(string userId, DateTimeOffset deletedAt)
    {
        _accounts.AddAccount(userId, userId + "@example.com");
        _users.Seed(NewUser(userId, deletedAt));

        return await SeedOwnedDataAsync(userId);
    }

    // An active account with the same kinds of data
    private async Task SeedActiveAccountAsync(string userId)
    {
        _accounts.AddAccount(userId, userId + "@example.com");
        _users.Seed(NewUser(userId, deletedAt: null));

        await SeedOwnedDataAsync(userId);
    }

    // Gives the account a profile with two images, one code per purpose, a reset token and an email change request
    private async Task<List<string>> SeedOwnedDataAsync(string userId)
    {
        var picture = (await _images.StoreAsync(MemberImageKind.ProfilePicture, new MemoryStream())).AssetId!;
        var banner = (await _images.StoreAsync(MemberImageKind.Banner, new MemoryStream())).AssetId!;

        await _profiles.CreateAsync(new Profile
        {
            Id = userId,
            DisplayName = "Jane",
            ProfilePictureAssetId = picture,
            BannerAssetId = banner
        });

        foreach (var purpose in Enum.GetValues<VerificationPurpose>())
        {
            await _codes.SaveAsync(new VerificationCode
            {
                Id = VerificationCode.BuildId(userId, purpose),
                UserId = userId,
                Purpose = purpose,
                CodeHash = "hash",
                Salt = "salt",
                CreatedAt = _clock.UtcNow,
                ExpiresAt = _clock.UtcNow.AddMinutes(10)
            });
        }

        await _resetTokens.SaveAsync(new PasswordResetToken
        {
            Id = "token-" + userId,
            UserId = userId,
            CreatedAt = _clock.UtcNow,
            ExpiresAt = _clock.UtcNow.AddMinutes(10)
        });

        await _emailChanges.SaveAsync(new EmailChangeRequest
        {
            Id = userId,
            NewEmail = "new-" + userId + "@example.com",
            CreatedAt = _clock.UtcNow,
            ExpiresAt = _clock.UtcNow.AddMinutes(10)
        });

        return new List<string> { picture, banner };
    }

    // Asserts that no pending code, reset token or email change request of the account remains
    private async Task AssertNoLeftoversAsync(string userId)
    {
        foreach (var purpose in Enum.GetValues<VerificationPurpose>())
        {
            (await _codes.GetAsync(userId, purpose)).ShouldBeNull();
        }

        _resetTokens.Find("token-" + userId).ShouldBeNull();
        _emailChanges.Find(userId).ShouldBeNull();
    }

    #endregion

    #region Nested Types

    // A user store whose claim finds the account already restored, as if the member signed in right after the listing
    private sealed class RestoringBeforeClaimUserStore : IUserStore
    {
        private readonly IUserStore _inner;

        public RestoringBeforeClaimUserStore(IUserStore inner)
        {
            _inner = inner;
        }

        public Task<User?> GetByIdAsync(string id) => _inner.GetByIdAsync(id);

        public Task CreateAsync(User user) => _inner.CreateAsync(user);

        public Task UpdateAsync(User user) => _inner.UpdateAsync(user);

        public Task ModifyAsync(string id, Action<User> modify) => _inner.ModifyAsync(id, modify);

        public Task<IReadOnlyList<User>> ListDeletedBeforeAsync(DateTimeOffset cutoff) =>
            _inner.ListDeletedBeforeAsync(cutoff);

        public async Task<bool> TryClaimForPurgeAsync(
            string id,
            DateTimeOffset cutoff,
            DateTimeOffset now,
            DateTimeOffset leaseUntil)
        {
            await _inner.ModifyAsync(id, user => user.DeletedAt = null);

            return await _inner.TryClaimForPurgeAsync(id, cutoff, now, leaseUntil);
        }

        public Task DeleteAsync(string id) => _inner.DeleteAsync(id);
    }

    #endregion
}
