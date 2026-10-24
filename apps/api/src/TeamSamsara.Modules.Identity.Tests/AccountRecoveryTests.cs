// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/AccountRecoveryTests.cs
// Version : 1.0.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Proves the rules for bringing a deleted account back: only within the recovery window,
// never while a purge holds it, and a live account is left alone.

using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Tests.Fakes;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Tests;

public class AccountRecoveryTests
{
    #region Fields

    private readonly FakeClock _clock = new();
    private readonly AuthenticationSettings _settings = new();
    private readonly AccountRecovery _recovery;

    #endregion

    #region Constructors

    public AccountRecoveryTests()
    {
        _recovery = new AccountRecovery(_clock, Options.Create(_settings));
    }

    #endregion

    #region Public Methods

    [Fact]
    public void IsPastWindow_ForALiveAccount_IsFalse()
    {
        _recovery.IsPastWindow(NewUser()).ShouldBeFalse();
    }

    [Fact]
    public void IsPastWindow_AtTheEndOfTheWindow_IsFalse()
    {
        var user = NewUser(deletedAt: _clock.UtcNow.AddDays(-_settings.RecoveryWindowDays));

        _recovery.IsPastWindow(user).ShouldBeFalse();
    }

    [Fact]
    public void IsPastWindow_OneSecondAfterTheWindow_IsTrue()
    {
        var user = NewUser(deletedAt: _clock.UtcNow.AddDays(-_settings.RecoveryWindowDays).AddSeconds(-1));

        _recovery.IsPastWindow(user).ShouldBeTrue();
    }

    [Fact]
    public void IsBeingPurged_OnlyWhileTheLeaseIsInTheFuture()
    {
        _recovery.IsBeingPurged(NewUser()).ShouldBeFalse();
        _recovery.IsBeingPurged(NewUser(purgeLeaseUntil: _clock.UtcNow.AddMinutes(-1))).ShouldBeFalse();
        _recovery.IsBeingPurged(NewUser(purgeLeaseUntil: _clock.UtcNow.AddMinutes(1))).ShouldBeTrue();
    }

    [Fact]
    public void TryRestore_WithinTheWindow_ClearsTheMarkers()
    {
        var user = NewUser(
            deletedAt: _clock.UtcNow.AddDays(-1),
            purgeLeaseUntil: _clock.UtcNow.AddMinutes(-5));

        _recovery.TryRestore(user).ShouldBeTrue();

        user.DeletedAt.ShouldBeNull();
        user.PurgeLeaseUntil.ShouldBeNull();
    }

    [Fact]
    public void TryRestore_PastTheWindow_ChangesNothing()
    {
        var deletedAt = _clock.UtcNow.AddDays(-_settings.RecoveryWindowDays).AddSeconds(-1);
        var user = NewUser(deletedAt: deletedAt);

        _recovery.TryRestore(user).ShouldBeFalse();

        user.DeletedAt.ShouldBe(deletedAt);
    }

    [Fact]
    public void TryRestore_WhileAPurgeHoldsTheAccount_ChangesNothing()
    {
        var deletedAt = _clock.UtcNow.AddDays(-1);
        var user = NewUser(deletedAt: deletedAt, purgeLeaseUntil: _clock.UtcNow.AddMinutes(5));

        _recovery.TryRestore(user).ShouldBeFalse();

        user.DeletedAt.ShouldBe(deletedAt);
        user.PurgeLeaseUntil.ShouldNotBeNull();
    }

    #endregion

    #region Private Methods

    private User NewUser(DateTimeOffset? deletedAt = null, DateTimeOffset? purgeLeaseUntil = null)
    {
        return new User
        {
            Id = "user-1",
            AccessLevel = AccessLevel.Member,
            CreatedAt = _clock.UtcNow.AddDays(-100),
            DeletedAt = deletedAt,
            PurgeLeaseUntil = purgeLeaseUntil
        };
    }

    #endregion
}
