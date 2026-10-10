// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/SignInTrackerTests.cs
// Version : 1.1.0
// Latest commit: fix/password-change-verified-sign-ins
// Author : Gerrah
// Purpose : Proves sign-in verification: only recorded sign-ins count, deleted or unknown
// members never verify, old sign-ins are trimmed, clearing forgets them all, and the cache is
// used and invalidated.

using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Tests.Fakes;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Tests;

public class SignInTrackerTests
{
    #region Fields

    private const string UserId = "user-1";
    private const long SignIn = 1_700_000_000;

    private readonly InMemoryUserStore _users = new();
    private readonly InMemoryCacheService _cache = new();
    private readonly FakeClock _clock = new();
    private readonly AuthenticationSettings _settings = new() { MaxVerifiedSignIns = 3 };
    private readonly SignInTracker _tracker;

    #endregion

    #region Constructors

    public SignInTrackerTests()
    {
        _tracker = new SignInTracker(_users, _cache, Options.Create(_settings));
    }

    #endregion

    #region Public Methods

    [Fact]
    public async Task ARecordedSignIn_IsVerified_AndAnotherOneIsNot()
    {
        _users.Seed(NewUser());

        await _tracker.RecordVerifiedAsync(UserId, SignIn);

        (await _tracker.IsVerifiedAsync(UserId, SignIn)).ShouldBeTrue();
        (await _tracker.IsVerifiedAsync(UserId, SignIn + 1)).ShouldBeFalse();
    }

    [Fact]
    public async Task ASignInNeverRecorded_IsNotVerified()
    {
        _users.Seed(NewUser());

        (await _tracker.IsVerifiedAsync(UserId, SignIn)).ShouldBeFalse();
    }

    [Fact]
    public async Task AnUnknownMember_IsNeverVerified()
    {
        (await _tracker.IsVerifiedAsync("nobody", SignIn)).ShouldBeFalse();
    }

    [Fact]
    public async Task ADeletedMember_IsNeverVerified_EvenWithARecordedSignIn()
    {
        var user = NewUser();
        user.DeletedAt = _clock.UtcNow;
        user.VerifiedSignIns.Add(SignIn);
        _users.Seed(user);

        (await _tracker.IsVerifiedAsync(UserId, SignIn)).ShouldBeFalse();
    }

    [Fact]
    public async Task Recording_BeyondTheLimit_DropsTheOldestSignIns()
    {
        _users.Seed(NewUser());

        for (long signIn = 1; signIn <= 4; signIn++)
        {
            await _tracker.RecordVerifiedAsync(UserId, signIn);
        }

        (await _tracker.IsVerifiedAsync(UserId, 1)).ShouldBeFalse();
        (await _tracker.IsVerifiedAsync(UserId, 2)).ShouldBeTrue();
        (await _tracker.IsVerifiedAsync(UserId, 3)).ShouldBeTrue();
        (await _tracker.IsVerifiedAsync(UserId, 4)).ShouldBeTrue();
    }

    [Fact]
    public async Task Recording_TheSameSignInTwice_StoresItOnce()
    {
        _users.Seed(NewUser());

        await _tracker.RecordVerifiedAsync(UserId, SignIn);
        await _tracker.RecordVerifiedAsync(UserId, SignIn);

        var stored = await _users.GetByIdAsync(UserId);
        stored.ShouldNotBeNull();
        stored.VerifiedSignIns.ShouldHaveSingleItem().ShouldBe(SignIn);
    }

    [Fact]
    public async Task Recording_ForAnUnknownMember_Throws()
    {
        await Should.ThrowAsync<InvalidOperationException>(
            () => _tracker.RecordVerifiedAsync("nobody", SignIn));
    }

    [Fact]
    public async Task Checking_Twice_ReadsTheStoreOnlyOnce()
    {
        _users.Seed(NewUser());

        await _tracker.IsVerifiedAsync(UserId, SignIn);
        await _tracker.IsVerifiedAsync(UserId, SignIn);

        _cache.SetCount.ShouldBe(1);
    }

    [Fact]
    public async Task Recording_ClearsTheCache_SoTheNewSignInIsSeenAtOnce()
    {
        _users.Seed(NewUser());
        (await _tracker.IsVerifiedAsync(UserId, SignIn)).ShouldBeFalse();

        await _tracker.RecordVerifiedAsync(UserId, SignIn);

        (await _tracker.IsVerifiedAsync(UserId, SignIn)).ShouldBeTrue();
    }

    [Fact]
    public async Task Clearing_ForgetsEverySignIn_AtOnce_EvenWhenTheyWereCached()
    {
        _users.Seed(NewUser());
        await _tracker.RecordVerifiedAsync(UserId, SignIn);
        await _tracker.RecordVerifiedAsync(UserId, SignIn + 1);
        (await _tracker.IsVerifiedAsync(UserId, SignIn)).ShouldBeTrue();

        await _tracker.ClearVerifiedAsync(UserId);

        (await _tracker.IsVerifiedAsync(UserId, SignIn)).ShouldBeFalse();
        (await _tracker.IsVerifiedAsync(UserId, SignIn + 1)).ShouldBeFalse();

        var stored = await _users.GetByIdAsync(UserId);
        stored.ShouldNotBeNull();
        stored.VerifiedSignIns.ShouldBeEmpty();
    }

    [Fact]
    public async Task Clearing_ForAnUnknownMember_Throws()
    {
        await Should.ThrowAsync<InvalidOperationException>(
            () => _tracker.ClearVerifiedAsync("nobody"));
    }

    #endregion

    #region Private Methods

    private User NewUser()
    {
        return new User { Id = UserId, AccessLevel = AccessLevel.Member, CreatedAt = _clock.UtcNow };
    }

    #endregion
}
