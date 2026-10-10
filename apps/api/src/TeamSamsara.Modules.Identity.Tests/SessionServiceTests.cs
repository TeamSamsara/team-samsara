// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/SessionServiceTests.cs
// Version : 1.0.0
// Latest commit: feat/logout
// Author : Gerrah
// Purpose : Proves signing out ends one sign-in without touching refresh tokens, and signing out everywhere ends them all.

using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Tests.Fakes;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Tests;

public class SessionServiceTests
{
    #region Fields

    private const string UserId = "user-1";
    private const string Email = "jane.doe@example.com";
    private const long ThisDevice = 1_700_000_000;
    private const long OtherDevice = 1_700_000_500;

    private readonly InMemoryUserStore _users = new();
    private readonly InMemoryCacheService _cache = new();
    private readonly FakeAccountGateway _accounts = new();
    private readonly FakeClock _clock = new();
    private readonly SignInTracker _signIns;
    private readonly SessionService _service;

    #endregion

    #region Constructors

    public SessionServiceTests()
    {
        _signIns = new SignInTracker(_users, _cache, Options.Create(new AuthenticationSettings()));
        _service = new SessionService(_signIns, _accounts);

        _accounts.AddAccount(UserId, Email);
        _users.Seed(new User
        {
            Id = UserId,
            AccessLevel = AccessLevel.Member,
            CreatedAt = _clock.UtcNow,
            VerifiedSignIns = new List<long> { ThisDevice, OtherDevice }
        });
    }

    #endregion

    #region Public Methods

    [Fact]
    public async Task SignOut_ForgetsThisSignIn_AtOnce()
    {
        (await _signIns.IsVerifiedAsync(UserId, ThisDevice)).ShouldBeTrue();

        await _service.SignOutAsync(UserId, ThisDevice);

        (await _signIns.IsVerifiedAsync(UserId, ThisDevice)).ShouldBeFalse();
    }

    [Fact]
    public async Task SignOut_KeepsTheOtherDevicesSignedIn()
    {
        await _service.SignOutAsync(UserId, ThisDevice);

        (await _signIns.IsVerifiedAsync(UserId, OtherDevice)).ShouldBeTrue();
    }

    [Fact]
    public async Task SignOut_DoesNotRevokeRefreshTokens()
    {
        await _service.SignOutAsync(UserId, ThisDevice);

        _accounts.RevokedSessions.ShouldBeEmpty();
    }

    [Fact]
    public async Task SignOut_Twice_IsHarmless()
    {
        await _service.SignOutAsync(UserId, ThisDevice);

        await Should.NotThrowAsync(() => _service.SignOutAsync(UserId, ThisDevice));

        (await _signIns.IsVerifiedAsync(UserId, OtherDevice)).ShouldBeTrue();
    }

    [Fact]
    public async Task SignOutEverywhere_ForgetsEverySignIn_AtOnce()
    {
        (await _signIns.IsVerifiedAsync(UserId, ThisDevice)).ShouldBeTrue();

        await _service.SignOutEverywhereAsync(UserId);

        (await _signIns.IsVerifiedAsync(UserId, ThisDevice)).ShouldBeFalse();
        (await _signIns.IsVerifiedAsync(UserId, OtherDevice)).ShouldBeFalse();
    }

    [Fact]
    public async Task SignOutEverywhere_RevokesTheRefreshTokens()
    {
        await _service.SignOutEverywhereAsync(UserId);

        _accounts.RevokedSessions.ShouldHaveSingleItem().ShouldBe(UserId);
    }

    #endregion
}
