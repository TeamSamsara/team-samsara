// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/ProfileServiceConcurrencyTests.cs
// Version : 1.0.0
// Latest commit: fix/profile-atomic-update
// Author : Gerrah
// Purpose : Proves a profile change is applied to the profile as it is when saved, so an image
// upload never overwrites a change made meanwhile or brings back a deleted profile.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Modules.Identity.Handlers;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Tests.Fakes;
using TeamSamsara.Shared.Assets;

namespace TeamSamsara.Modules.Identity.Tests;

public class ProfileServiceConcurrencyTests
{
    #region Fields

    private const string UserId = "user-1";
    private const string DisplayName = "Original";
    private const string BannerFromElsewhere = "banner-from-elsewhere";

    private readonly InMemoryProfileStore _profiles = new();
    private readonly FakeMemberImageService _images = new();
    private readonly ProfileService _service;

    #endregion

    #region Constructors

    public ProfileServiceConcurrencyTests()
    {
        var settings = new ProfileSettings
        {
            DisplayNameMinLength = 2,
            DisplayNameMaxLength = 10,
            BioMaxLength = 20
        };

        _service = new ProfileService(
            _profiles, _images, Options.Create(settings), NullLogger<ProfileService>.Instance);
    }

    #endregion

    #region Public Methods

    [Fact]
    public async Task SettingAPicture_WhileTheBannerChanges_KeepsTheNewBanner()
    {
        await SeedAsync();
        _images.BeforeStore = async () =>
        {
            await _profiles.DeleteAsync(UserId);
            await _profiles.CreateAsync(new Profile
            {
                Id = UserId,
                DisplayName = DisplayName,
                BannerAssetId = BannerFromElsewhere
            });
        };

        var result = await _service.SetImageAsync(UserId, MemberImageKind.ProfilePicture, new MemoryStream());

        result.Status.ShouldBe(ProfileStatus.Success);
        var saved = (await _profiles.GetByIdAsync(UserId)).ShouldNotBeNull();
        saved.ProfilePictureAssetId.ShouldBe("asset-1");
        saved.BannerAssetId.ShouldBe(BannerFromElsewhere);
    }

    [Fact]
    public async Task SettingAnImage_WhenTheProfileIsDeletedMeanwhile_ReportsNotFound_AndRemovesTheStoredImage()
    {
        await SeedAsync();
        _images.BeforeStore = () => _profiles.DeleteAsync(UserId);

        var result = await _service.SetImageAsync(UserId, MemberImageKind.ProfilePicture, new MemoryStream());

        result.Status.ShouldBe(ProfileStatus.ProfileNotFound);
        (await _profiles.GetByIdAsync(UserId)).ShouldBeNull();
        _images.StoredAssetIds.ShouldBeEmpty();
        _images.DeletedAssetIds.ShouldBe(new[] { "asset-1" });
    }

    #endregion

    #region Private Methods

    private Task SeedAsync()
    {
        return _profiles.CreateAsync(new Profile { Id = UserId, DisplayName = DisplayName });
    }

    #endregion
}
