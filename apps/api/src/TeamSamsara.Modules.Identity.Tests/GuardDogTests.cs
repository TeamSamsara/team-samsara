// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/GuardDogTests.cs
// Version : 1.0.0
// Latest commit: feature/identity-module
// Author : Gerrah
// Purpose : Proves GuardDog recognizes clients: a stable keyed fingerprint of IP + user agent,
// remembering clients, refreshing them, and trimming to the configured limit.

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Tests.Fakes;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Tests;

public class GuardDogTests
{
    #region Fields

    private const string Ip = "203.0.113.7";
    private const string UserAgent = "TestBrowser/1.0";

    private readonly HttpContextAccessor _accessor = new();
    private readonly FakeClock _clock = new();
    private readonly GuardDogSettings _settings = new() { Secret = "test-secret-1234567890" };

    #endregion

    #region Public Methods

    [Fact]
    public void Inspect_ReturnsTheAddressAndUserAgent()
    {
        SetRequest(Ip, UserAgent);

        var client = CreateGuardDog().Inspect();

        client.IpAddress.ShouldBe(Ip);
        client.UserAgent.ShouldBe(UserAgent);
    }

    [Fact]
    public void Inspect_SameClient_GivesTheSameFingerprint()
    {
        SetRequest(Ip, UserAgent);
        var guardDog = CreateGuardDog();

        var first = guardDog.Inspect();
        var second = guardDog.Inspect();

        second.Fingerprint.ShouldBe(first.Fingerprint);
    }

    [Fact]
    public void Inspect_DifferentAddress_GivesADifferentFingerprint()
    {
        var guardDog = CreateGuardDog();

        SetRequest(Ip, UserAgent);
        var first = guardDog.Inspect();
        SetRequest("198.51.100.9", UserAgent);
        var second = guardDog.Inspect();

        second.Fingerprint.ShouldNotBe(first.Fingerprint);
    }

    [Fact]
    public void Inspect_DifferentUserAgent_GivesADifferentFingerprint()
    {
        var guardDog = CreateGuardDog();

        SetRequest(Ip, UserAgent);
        var first = guardDog.Inspect();
        SetRequest(Ip, "OtherBrowser/2.0");
        var second = guardDog.Inspect();

        second.Fingerprint.ShouldNotBe(first.Fingerprint);
    }

    [Fact]
    public void Inspect_DifferentSecret_GivesADifferentFingerprint()
    {
        SetRequest(Ip, UserAgent);
        var first = CreateGuardDog().Inspect();

        _settings.Secret = "another-secret-0987654321";
        var second = CreateGuardDog().Inspect();

        second.Fingerprint.ShouldNotBe(first.Fingerprint);
    }

    [Fact]
    public void Inspect_FingerprintDoesNotExposeTheRawValues()
    {
        SetRequest(Ip, UserAgent);

        var client = CreateGuardDog().Inspect();

        client.Fingerprint.ShouldNotContain(Ip);
        client.Fingerprint.ShouldNotContain(UserAgent);
    }

    [Fact]
    public void Inspect_WithoutAUserAgent_UsesUnknown()
    {
        SetRequest(Ip, null);

        var client = CreateGuardDog().Inspect();

        client.UserAgent.ShouldBe("unknown");
    }

    [Fact]
    public void Inspect_WithoutARequest_Throws()
    {
        var guardDog = CreateGuardDog();

        Should.Throw<InvalidOperationException>(() => guardDog.Inspect());
    }

    [Fact]
    public void IsKnown_IsFalseForAClientNeverRemembered()
    {
        SetRequest(Ip, UserAgent);
        var guardDog = CreateGuardDog();

        guardDog.IsKnown(NewUser(), guardDog.Inspect()).ShouldBeFalse();
    }

    [Fact]
    public void Remember_MakesTheClientKnown()
    {
        SetRequest(Ip, UserAgent);
        var guardDog = CreateGuardDog();
        var user = NewUser();
        var client = guardDog.Inspect();

        guardDog.Remember(user, client);

        guardDog.IsKnown(user, client).ShouldBeTrue();
    }

    [Fact]
    public void Remember_TheSameClientTwice_KeepsOneEntryAndRefreshesWhenItWasSeen()
    {
        SetRequest(Ip, UserAgent);
        var guardDog = CreateGuardDog();
        var user = NewUser();
        var client = guardDog.Inspect();

        guardDog.Remember(user, client);
        _clock.Advance(TimeSpan.FromHours(1));
        guardDog.Remember(user, client);

        var entry = user.KnownFingerprints.ShouldHaveSingleItem();
        entry.LastSeenAt.ShouldBe(_clock.UtcNow);
    }

    [Fact]
    public void Remember_BeyondTheLimit_DropsTheLeastRecentlySeen()
    {
        _settings.MaxKnownFingerprints = 2;
        var guardDog = CreateGuardDog();
        var user = NewUser();

        var oldest = RememberFrom(guardDog, user, "198.51.100.1");
        _clock.Advance(TimeSpan.FromMinutes(1));
        var middle = RememberFrom(guardDog, user, "198.51.100.2");
        _clock.Advance(TimeSpan.FromMinutes(1));
        var newest = RememberFrom(guardDog, user, "198.51.100.3");

        user.KnownFingerprints.Count.ShouldBe(2);
        guardDog.IsKnown(user, oldest).ShouldBeFalse();
        guardDog.IsKnown(user, middle).ShouldBeTrue();
        guardDog.IsKnown(user, newest).ShouldBeTrue();
    }

    #endregion

    #region Private Methods

    private GuardDog CreateGuardDog()
    {
        return new GuardDog(_accessor, _clock, Options.Create(_settings));
    }

    // Makes the "current request" come from this address and user agent
    private void SetRequest(string ip, string? userAgent)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ip);

        if (userAgent is not null)
        {
            context.Request.Headers.UserAgent = userAgent;
        }

        _accessor.HttpContext = context;
    }

    private User NewUser()
    {
        return new User { Id = "user-1", AccessLevel = AccessLevel.Member, CreatedAt = _clock.UtcNow };
    }

    // Remembers a client coming from the given address and returns it
    private ClientInfo RememberFrom(GuardDog guardDog, User user, string ip)
    {
        SetRequest(ip, UserAgent);
        var client = guardDog.Inspect();
        guardDog.Remember(user, client);

        return client;
    }

    #endregion
}
