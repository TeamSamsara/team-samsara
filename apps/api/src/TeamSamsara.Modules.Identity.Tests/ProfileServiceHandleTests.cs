// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/ProfileServiceHandleTests.cs
// Version : 1.0.0
// Latest commit: task/api-profile-onboarding
// Author : Gerrah
// Purpose : Proves handles and onboarding: automatic assignment, validation, uniqueness, the rename cooldown, the availability check, onboarding completion and the public lookup.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Tests.Fakes;

namespace TeamSamsara.Modules.Identity.Tests;

public class ProfileServiceHandleTests
{
    #region Fields

    private const string UserId = "user-1";
    private const string OtherUserId = "user-2";

    private readonly InMemoryProfileStore _profiles = new();
    private readonly InMemoryHandleStore _handles;
    private readonly FakeClock _clock = new();
    private readonly ProfileService _service;

    #endregion

    #region Constructors

    public ProfileServiceHandleTests()
    {
        _handles = new InMemoryHandleStore(_profiles);
        _service = new ProfileService(
            _profiles,
            _handles,
            new FakeMemberImageService(),
            Options.Create(new ProfileSettings()),
            _clock,
            NullLogger<ProfileService>.Instance);
    }

    #endregion

    #region Public Methods

    [Fact]
    public async Task AProfileWithoutAHandle_GetsOneFromItsDisplayName()
    {
        await SeedAsync(UserId, "Gerrah");

        var result = await _service.GetAsync(UserId);

        result.Profile.ShouldNotBeNull().Handle.ShouldBe("gerrah");
    }

    [Fact]
    public async Task ADisplayNameWithAccentsAndSpaces_BecomesACleanHandle()
    {
        await SeedAsync(UserId, "Éloïse  Dupont!");

        var result = await _service.GetAsync(UserId);

        result.Profile.ShouldNotBeNull().Handle.ShouldBe("eloisedupont");
    }

    [Fact]
    public async Task ATakenDisplayNameHandle_GetsNumbered()
    {
        await SeedAsync(OtherUserId, "Gerrah");
        await _service.GetAsync(OtherUserId);
        await SeedAsync(UserId, "Gerrah");

        var result = await _service.GetAsync(UserId);

        var handle = result.Profile.ShouldNotBeNull().Handle.ShouldNotBeNull();
        handle.ShouldStartWith("gerrah");
        handle.ShouldNotBe("gerrah");
        handle.Length.ShouldBe("gerrah".Length + 4);
    }

    [Fact]
    public async Task TheNeutralDisplayName_GetsANeutralNumberedHandle()
    {
        await SeedAsync(UserId, "Member");

        var result = await _service.GetAsync(UserId);

        var handle = result.Profile.ShouldNotBeNull().Handle.ShouldNotBeNull();
        handle.ShouldStartWith("member");
        handle.Length.ShouldBe("member".Length + 4);
    }

    [Fact]
    public async Task AHandleOnceAssigned_IsNotChangedByLaterReads()
    {
        await SeedAsync(UserId, "Gerrah");
        await _service.GetAsync(UserId);

        var result = await _service.GetAsync(UserId);

        result.Profile.ShouldNotBeNull().Handle.ShouldBe("gerrah");
    }

    [Fact]
    public async Task ChoosingAHandle_NormalizesItAndSavesIt()
    {
        await SeedAsync(UserId, "Gerrah");

        var result = await _service.SetHandleAsync(UserId, new SetHandleRequest("  @Samsara_Fan "));

        result.Status.ShouldBe(ProfileStatus.Success);
        result.Profile.ShouldNotBeNull().Handle.ShouldBe("samsara_fan");
        (await _handles.GetOwnerAsync("samsara_fan")).ShouldBe(UserId);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("_abc")]
    [InlineData("abc_")]
    [InlineData("has space")]
    [InlineData("bad-char")]
    [InlineData("émile")]
    [InlineData("this_handle_is_way_too_long")]
    [InlineData("")]
    public async Task AnInvalidHandle_IsRefused(string handle)
    {
        await SeedAsync(UserId, "Gerrah");

        var result = await _service.SetHandleAsync(UserId, new SetHandleRequest(handle));

        result.Status.ShouldBe(ProfileStatus.InvalidHandle);
        result.Profile.ShouldBeNull();
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("xfuckx")]
    public async Task AReservedOrBlockedHandle_IsRefusedAsTaken(string handle)
    {
        await SeedAsync(UserId, "Gerrah");

        var result = await _service.SetHandleAsync(UserId, new SetHandleRequest(handle));

        result.Status.ShouldBe(ProfileStatus.HandleTaken);
    }

    [Fact]
    public async Task AHandleAnotherMemberHolds_IsRefused()
    {
        await SeedAsync(OtherUserId, "Gerrah");
        await _service.GetAsync(OtherUserId);
        await SeedAsync(UserId, "Someone");

        var result = await _service.SetHandleAsync(UserId, new SetHandleRequest("GERRAH"));

        result.Status.ShouldBe(ProfileStatus.HandleTaken);
    }

    [Fact]
    public async Task ChangingTheHandle_FreesTheOldOne()
    {
        await SeedAsync(UserId, "Gerrah");
        await _service.GetAsync(UserId);

        await _service.SetHandleAsync(UserId, new SetHandleRequest("newname"));

        (await _handles.GetOwnerAsync("gerrah")).ShouldBeNull();
        (await _handles.GetOwnerAsync("newname")).ShouldBe(UserId);
    }

    [Fact]
    public async Task SettingTheHandle_OfAMissingProfile_ReportsNotFound()
    {
        var result = await _service.SetHandleAsync(UserId, new SetHandleRequest("newname"));

        result.Status.ShouldBe(ProfileStatus.ProfileNotFound);
    }

    [Fact]
    public async Task KeepingTheCurrentHandle_SucceedsWithoutStartingTheCooldown()
    {
        await SeedAsync(UserId, "Gerrah");
        await _service.GetAsync(UserId);

        var same = await _service.SetHandleAsync(UserId, new SetHandleRequest("gerrah"));
        var change = await _service.SetHandleAsync(UserId, new SetHandleRequest("newname"));

        same.Status.ShouldBe(ProfileStatus.Success);
        change.Status.ShouldBe(ProfileStatus.Success);
    }

    [Fact]
    public async Task TheFirstManualRename_IsFree_ButTheNextOneWaitsForTheCooldown()
    {
        await SeedAsync(UserId, "Gerrah");
        await _service.GetAsync(UserId);

        var first = await _service.SetHandleAsync(UserId, new SetHandleRequest("newname"));
        _clock.Advance(TimeSpan.FromDays(29));
        var tooSoon = await _service.SetHandleAsync(UserId, new SetHandleRequest("another"));

        first.Status.ShouldBe(ProfileStatus.Success);
        tooSoon.Status.ShouldBe(ProfileStatus.HandleCooldown);
        (await _handles.GetOwnerAsync("newname")).ShouldBe(UserId);
    }

    [Fact]
    public async Task AfterTheCooldown_TheHandleCanChangeAgain()
    {
        await SeedAsync(UserId, "Gerrah");
        await _service.GetAsync(UserId);
        await _service.SetHandleAsync(UserId, new SetHandleRequest("newname"));

        _clock.Advance(TimeSpan.FromDays(31));
        var result = await _service.SetHandleAsync(UserId, new SetHandleRequest("another"));

        result.Status.ShouldBe(ProfileStatus.Success);
        result.Profile.ShouldNotBeNull().Handle.ShouldBe("another");
    }

    [Fact]
    public async Task TheAvailabilityCheck_ReportsFreeTakenOwnReservedAndInvalidHandles()
    {
        await SeedAsync(OtherUserId, "Taken");
        await _service.GetAsync(OtherUserId);
        await SeedAsync(UserId, "Gerrah");
        await _service.GetAsync(UserId);

        (await _service.CheckHandleAsync(UserId, "freename")).ShouldBe(ProfileStatus.Success);
        (await _service.CheckHandleAsync(UserId, "@Taken")).ShouldBe(ProfileStatus.HandleTaken);
        (await _service.CheckHandleAsync(UserId, "gerrah")).ShouldBe(ProfileStatus.Success);
        (await _service.CheckHandleAsync(UserId, "admin")).ShouldBe(ProfileStatus.HandleTaken);
        (await _service.CheckHandleAsync(UserId, "no")).ShouldBe(ProfileStatus.InvalidHandle);
    }

    [Fact]
    public async Task TheAvailabilityCheck_ChangesNothing()
    {
        await SeedAsync(UserId, "Gerrah");
        await _service.GetAsync(UserId);

        await _service.CheckHandleAsync(UserId, "freename");

        (await _handles.GetOwnerAsync("freename")).ShouldBeNull();
        (await _handles.GetOwnerAsync("gerrah")).ShouldBe(UserId);
    }

    [Fact]
    public async Task CompletingOnboarding_SetsTheFlag_AndRepeatingItChangesNothing()
    {
        await SeedAsync(UserId, "Gerrah");

        var first = await _service.CompleteOnboardingAsync(UserId);
        var second = await _service.CompleteOnboardingAsync(UserId);

        first.Profile.ShouldNotBeNull().OnboardingCompleted.ShouldBeTrue();
        second.Profile.ShouldNotBeNull().OnboardingCompleted.ShouldBeTrue();
    }

    [Fact]
    public async Task ANewProfile_HasNotCompletedOnboarding()
    {
        await SeedAsync(UserId, "Gerrah");

        var result = await _service.GetAsync(UserId);

        result.Profile.ShouldNotBeNull().OnboardingCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task CompletingOnboarding_OfAMissingProfile_ReportsNotFound()
    {
        var result = await _service.CompleteOnboardingAsync(UserId);

        result.Status.ShouldBe(ProfileStatus.ProfileNotFound);
    }

    [Fact]
    public async Task APublicLookup_ReturnsWhatVisitorsMaySee()
    {
        await _profiles.CreateAsync(new Profile
        {
            Id = UserId,
            DisplayName = "Gerrah",
            Bio = "Hello",
            ProfilePictureAssetId = "pic",
            BannerAssetId = "banner"
        });
        await _service.GetAsync(UserId);

        var view = await _service.GetPublicAsync("@GERRAH");

        view.ShouldBe(new PublicProfileView("Gerrah", "gerrah", "Hello", "pic", "banner"));
    }

    [Theory]
    [InlineData("nobody")]
    [InlineData("x")]
    [InlineData("")]
    public async Task APublicLookup_OfAnUnknownOrInvalidHandle_ReturnsNothing(string handle)
    {
        (await _service.GetPublicAsync(handle)).ShouldBeNull();
    }

    #endregion

    #region Private Methods

    private Task SeedAsync(string userId, string displayName)
    {
        return _profiles.CreateAsync(new Profile { Id = userId, DisplayName = displayName });
    }

    #endregion
}
