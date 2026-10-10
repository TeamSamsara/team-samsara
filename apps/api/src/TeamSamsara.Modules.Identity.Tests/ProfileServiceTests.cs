// File: /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/ProfileServiceTests.cs
// Version: 1.0.0
// Latest commit: feature/identity-profile
// Author: Gerrah
//
// Purpose: Proves profile editing: text validation, image replacement, rollback and clearing.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Tests.Fakes;
using TeamSamsara.Shared.Assets;

namespace TeamSamsara.Modules.Identity.Tests;

public class ProfileServiceTests
{
    #region Fields

    private const string UserId = "user-1";
    private const string OriginalName = "Original";
    private const string OriginalBio = "Old bio";

    private readonly InMemoryProfileStore _profiles = new();
    private readonly ThrowingProfileStore _store;
    private readonly FakeMemberImageService _images = new();
    private readonly ProfileService _service;

    #endregion

    #region Constructors

    public ProfileServiceTests()
    {
        var settings = new ProfileSettings
        {
            DisplayNameMinLength = 2,
            DisplayNameMaxLength = 10,
            BioMaxLength = 20
        };

        _store = new ThrowingProfileStore(_profiles);
        _service = new ProfileService(
            _store, _images, Options.Create(settings), NullLogger<ProfileService>.Instance);
    }

    #endregion

    #region Public Methods

    [Fact]
    public async Task Getting_ReturnsTheMembersProfile()
    {
        await SeedAsync();

        var result = await _service.GetAsync(UserId);

        result.Status.ShouldBe(ProfileStatus.Success);
        var profile = result.Profile.ShouldNotBeNull();
        profile.DisplayName.ShouldBe(OriginalName);
        profile.Bio.ShouldBe(OriginalBio);
    }

    [Fact]
    public async Task Getting_AMissingProfile_ReportsNotFound()
    {
        var result = await _service.GetAsync(UserId);

        result.Status.ShouldBe(ProfileStatus.ProfileNotFound);
        result.Profile.ShouldBeNull();
    }

    [Fact]
    public async Task Updating_SavesTheTrimmedNameAndBio()
    {
        await SeedAsync();

        var result = await _service.UpdateAsync(UserId, new UpdateProfileRequest("  Felix  ", "  Hello there  "));

        result.Status.ShouldBe(ProfileStatus.Success);
        var stored = (await _profiles.GetByIdAsync(UserId)).ShouldNotBeNull();
        stored.DisplayName.ShouldBe("Felix");
        stored.Bio.ShouldBe("Hello there");
    }

    [Fact]
    public async Task Updating_WithABlankBio_ClearsIt()
    {
        await SeedAsync();

        var result = await _service.UpdateAsync(UserId, new UpdateProfileRequest("Felix", "   "));

        result.Status.ShouldBe(ProfileStatus.Success);
        (await _profiles.GetByIdAsync(UserId)).ShouldNotBeNull().Bio.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a")]
    [InlineData("ABCDEFGHIJK")]
    [InlineData("bad\nname")]
    public async Task Updating_WithAnInvalidName_IsRefused_AndChangesNothing(string? displayName)
    {
        await SeedAsync();

        var result = await _service.UpdateAsync(UserId, new UpdateProfileRequest(displayName!, "New bio"));

        result.Status.ShouldBe(ProfileStatus.InvalidDisplayName);
        result.Profile.ShouldBeNull();
        var stored = (await _profiles.GetByIdAsync(UserId)).ShouldNotBeNull();
        stored.DisplayName.ShouldBe(OriginalName);
        stored.Bio.ShouldBe(OriginalBio);
    }

    [Fact]
    public async Task ANameIsMeasuredInVisibleCharacters_SoTenEmojiFitALimitOfTen()
    {
        await SeedAsync();
        var tenEmoji = string.Concat(Enumerable.Repeat("\U0001F600", 10));

        var result = await _service.UpdateAsync(UserId, new UpdateProfileRequest(tenEmoji, null));

        result.Status.ShouldBe(ProfileStatus.Success);
    }

    [Theory]
    [InlineData("012345678901234567890")]
    [InlineData("bad\tbio")]
    public async Task Updating_WithAnInvalidBio_IsRefused_AndChangesNothing(string bio)
    {
        await SeedAsync();

        var result = await _service.UpdateAsync(UserId, new UpdateProfileRequest("Felix", bio));

        result.Status.ShouldBe(ProfileStatus.InvalidBio);
        var stored = (await _profiles.GetByIdAsync(UserId)).ShouldNotBeNull();
        stored.DisplayName.ShouldBe(OriginalName);
        stored.Bio.ShouldBe(OriginalBio);
    }

    [Fact]
    public async Task ABioMayContainLineBreaks()
    {
        await SeedAsync();

        var result = await _service.UpdateAsync(UserId, new UpdateProfileRequest("Felix", "line one\nline two"));

        result.Status.ShouldBe(ProfileStatus.Success);
    }

    [Fact]
    public async Task Updating_AMissingProfile_ReportsNotFound()
    {
        var result = await _service.UpdateAsync(UserId, new UpdateProfileRequest("Felix", null));

        result.Status.ShouldBe(ProfileStatus.ProfileNotFound);
    }

    [Fact]
    public async Task SettingAPicture_StoresItAndLinksItToTheProfile()
    {
        await SeedAsync();

        var result = await _service.SetImageAsync(UserId, MemberImageKind.ProfilePicture, Content());

        result.Status.ShouldBe(ProfileStatus.Success);
        var profile = result.Profile.ShouldNotBeNull();
        profile.ProfilePictureAssetId.ShouldBe("asset-1");
        profile.BannerAssetId.ShouldBeNull();
        (await _profiles.GetByIdAsync(UserId)).ShouldNotBeNull().ProfilePictureAssetId.ShouldBe("asset-1");
    }

    [Fact]
    public async Task SettingABanner_LinksTheBannerAndLeavesThePictureAlone()
    {
        await SeedAsync();

        var result = await _service.SetImageAsync(UserId, MemberImageKind.Banner, Content());

        var profile = result.Profile.ShouldNotBeNull();
        profile.BannerAssetId.ShouldBe("asset-1");
        profile.ProfilePictureAssetId.ShouldBeNull();
    }

    [Fact]
    public async Task ReplacingAnImage_LinksTheNewOne_AndDeletesTheOldOne()
    {
        await SeedAsync();
        await _service.SetImageAsync(UserId, MemberImageKind.ProfilePicture, Content());

        var result = await _service.SetImageAsync(UserId, MemberImageKind.ProfilePicture, Content());

        result.Profile.ShouldNotBeNull().ProfilePictureAssetId.ShouldBe("asset-2");
        _images.StoredAssetIds.ShouldBe(new[] { "asset-2" });
        _images.DeletedAssetIds.ShouldBe(new[] { "asset-1" });
    }

    [Theory]
    [InlineData(MemberImageStatus.UnsupportedType, ProfileStatus.ImageUnsupportedType)]
    [InlineData(MemberImageStatus.TooLarge, ProfileStatus.ImageTooLarge)]
    public async Task ARefusedImage_ReportsWhy_AndChangesNothing(
        MemberImageStatus imageStatus,
        ProfileStatus expected)
    {
        await SeedAsync();
        _images.NextStatus = imageStatus;

        var result = await _service.SetImageAsync(UserId, MemberImageKind.ProfilePicture, Content());

        result.Status.ShouldBe(expected);
        _images.StoredAssetIds.ShouldBeEmpty();
        (await _profiles.GetByIdAsync(UserId)).ShouldNotBeNull().ProfilePictureAssetId.ShouldBeNull();
    }

    [Fact]
    public async Task SettingAnImage_ForAMissingProfile_ReportsNotFound_AndStoresNothing()
    {
        var result = await _service.SetImageAsync(UserId, MemberImageKind.ProfilePicture, Content());

        result.Status.ShouldBe(ProfileStatus.ProfileNotFound);
        _images.StoredAssetIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task WhenSavingTheProfileFails_TheNewImageIsRemoved_AndTheErrorIsRaised()
    {
        await SeedAsync();
        await _service.SetImageAsync(UserId, MemberImageKind.ProfilePicture, Content());
        _store.FailUpdates = true;

        await Should.ThrowAsync<InvalidOperationException>(
            () => _service.SetImageAsync(UserId, MemberImageKind.ProfilePicture, Content()));

        _images.StoredAssetIds.ShouldBe(new[] { "asset-1" });
        (await _profiles.GetByIdAsync(UserId)).ShouldNotBeNull().ProfilePictureAssetId.ShouldBe("asset-1");
    }

    [Fact]
    public async Task WhenDeletingTheOldImageFails_TheReplacementStillSucceeds()
    {
        await SeedAsync();
        await _service.SetImageAsync(UserId, MemberImageKind.ProfilePicture, Content());
        _images.FailDeletes = true;

        var result = await _service.SetImageAsync(UserId, MemberImageKind.ProfilePicture, Content());

        result.Status.ShouldBe(ProfileStatus.Success);
        result.Profile.ShouldNotBeNull().ProfilePictureAssetId.ShouldBe("asset-2");
    }

    [Fact]
    public async Task ClearingAnImage_UnlinksIt_AndDeletesIt()
    {
        await SeedAsync();
        await _service.SetImageAsync(UserId, MemberImageKind.Banner, Content());

        var result = await _service.ClearImageAsync(UserId, MemberImageKind.Banner);

        result.Status.ShouldBe(ProfileStatus.Success);
        result.Profile.ShouldNotBeNull().BannerAssetId.ShouldBeNull();
        (await _profiles.GetByIdAsync(UserId)).ShouldNotBeNull().BannerAssetId.ShouldBeNull();
        _images.StoredAssetIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task ClearingAnEmptySlot_SucceedsAndDeletesNothing()
    {
        await SeedAsync();

        var result = await _service.ClearImageAsync(UserId, MemberImageKind.Banner);

        result.Status.ShouldBe(ProfileStatus.Success);
        _images.DeletedAssetIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task ClearingAnImage_ForAMissingProfile_ReportsNotFound()
    {
        var result = await _service.ClearImageAsync(UserId, MemberImageKind.Banner);

        result.Status.ShouldBe(ProfileStatus.ProfileNotFound);
    }

    [Fact]
    public async Task WhenDeletingAClearedImageFails_ClearingStillSucceeds()
    {
        await SeedAsync();
        await _service.SetImageAsync(UserId, MemberImageKind.Banner, Content());
        _images.FailDeletes = true;

        var result = await _service.ClearImageAsync(UserId, MemberImageKind.Banner);

        result.Status.ShouldBe(ProfileStatus.Success);
        result.Profile.ShouldNotBeNull().BannerAssetId.ShouldBeNull();
    }

    #endregion

    #region Private Methods

    // Creates the member's profile with a known name and bio
    private Task SeedAsync()
    {
        return _profiles.CreateAsync(new Profile
        {
            Id = UserId,
            DisplayName = OriginalName,
            Bio = OriginalBio
        });
    }

    // Image bytes the fake service ignores
    private static MemoryStream Content() => new();

    #endregion
}
