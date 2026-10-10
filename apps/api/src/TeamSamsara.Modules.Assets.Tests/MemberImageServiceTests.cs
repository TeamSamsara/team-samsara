// File: /team-samsara/apps/api/src/TeamSamsara.Modules.Assets.Tests/MemberImageServiceTests.cs
// Version: 1.1.0
// Latest commit: feat/account-purge
// Author: Gerrah
//
// Purpose: Proves member image storage: formats, per-kind size limits, rejections, and deletion that never leaves an orphan file and can always be retried.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Modules.Assets.Models;
using TeamSamsara.Modules.Assets.Services;
using TeamSamsara.Modules.Assets.Tests.Fakes;
using TeamSamsara.Shared.Assets;
using TeamSamsara.Shared.Http;
using TeamSamsara.Shared.Results;

namespace TeamSamsara.Modules.Assets.Tests;

public class MemberImageServiceTests
{
    #region Fields

    private const int PictureLimit = 100;
    private const int BannerLimit = 200;

    private readonly InMemoryAssetStorageService _storage = new();
    private readonly InMemoryAssetMetadataStore _metadata = new();
    private readonly MemberImageService _service;

    #endregion

    #region Constructors

    public MemberImageServiceTests()
    {
        _service = CreateService(new MemberImageSettings
        {
            ProfilePictureMaxBytes = PictureLimit,
            BannerMaxBytes = BannerLimit
        });
    }

    #endregion

    #region Public Methods

    [Fact]
    public async Task AValidProfilePicture_IsStored_AndItsAssetIdIsReturned()
    {
        var result = await _service.StoreAsync(MemberImageKind.ProfilePicture, ImageStream("png", 50));

        result.Status.ShouldBe(MemberImageStatus.Success);
        result.AssetId.ShouldNotBeNull();

        var metadata = _metadata.Assets[result.AssetId];
        metadata.type.ShouldBe(AssetType.Image);
        metadata.Alt.ShouldBe("Profile picture");
        _storage.Files.ContainsKey(metadata.FileRelativePath).ShouldBeTrue();
    }

    [Fact]
    public async Task ABanner_IsStoredWithBannerAltText()
    {
        var result = await _service.StoreAsync(MemberImageKind.Banner, ImageStream("png", 50));

        result.Status.ShouldBe(MemberImageStatus.Success);
        _metadata.Assets[result.AssetId!].Alt.ShouldBe("Banner");
    }

    [Theory]
    [InlineData("png", "image/png", ".png")]
    [InlineData("jpeg", "image/jpeg", ".jpg")]
    [InlineData("webp", "image/webp", ".webp")]
    public async Task EachAcceptedFormat_IsStoredWithItsRealContentTypeAndExtension(
        string format,
        string contentType,
        string extension)
    {
        var result = await _service.StoreAsync(MemberImageKind.ProfilePicture, ImageStream(format, 50));

        result.Status.ShouldBe(MemberImageStatus.Success);

        var metadata = _metadata.Assets[result.AssetId!];
        metadata.ContentType.ShouldBe(contentType);
        metadata.FileRelativePath.ShouldEndWith(extension);
        _storage.Files[metadata.FileRelativePath].ContentType.ShouldBe(contentType);
    }

    [Fact]
    public async Task AnImageAtExactlyTheLimit_IsAccepted()
    {
        var result = await _service.StoreAsync(MemberImageKind.ProfilePicture, ImageStream("png", PictureLimit));

        result.Status.ShouldBe(MemberImageStatus.Success);
    }

    [Fact]
    public async Task AnImageOverTheLimit_IsRejected_AndNothingIsStored()
    {
        var result = await _service.StoreAsync(MemberImageKind.ProfilePicture, ImageStream("png", PictureLimit + 1));

        result.Status.ShouldBe(MemberImageStatus.TooLarge);
        result.AssetId.ShouldBeNull();
        _storage.Files.ShouldBeEmpty();
        _metadata.Assets.ShouldBeEmpty();
    }

    [Fact]
    public async Task TheSameImage_CanBeABannerButNotAProfilePicture()
    {
        var size = PictureLimit + 50;

        var asBanner = await _service.StoreAsync(MemberImageKind.Banner, ImageStream("png", size));
        var asPicture = await _service.StoreAsync(MemberImageKind.ProfilePicture, ImageStream("png", size));

        asBanner.Status.ShouldBe(MemberImageStatus.Success);
        asPicture.Status.ShouldBe(MemberImageStatus.TooLarge);
    }

    [Fact]
    public async Task AFileThatIsNotAnImage_IsRejected_AndNothingIsStored()
    {
        var result = await _service.StoreAsync(MemberImageKind.ProfilePicture, new MemoryStream(NotAnImage(50)));

        result.Status.ShouldBe(MemberImageStatus.UnsupportedType);
        result.AssetId.ShouldBeNull();
        _storage.Files.ShouldBeEmpty();
        _metadata.Assets.ShouldBeEmpty();
    }

    [Fact]
    public async Task ATooLargeFile_ReportsTooLarge_EvenWhenItIsNotAnImage()
    {
        var result = await _service.StoreAsync(MemberImageKind.ProfilePicture, new MemoryStream(NotAnImage(PictureLimit + 1)));

        result.Status.ShouldBe(MemberImageStatus.TooLarge);
    }

    [Fact]
    public async Task AnUploadThatNeedsSeveralReads_IsStillRejectedWhenTooLarge()
    {
        const int limit = 100_000;
        var service = CreateService(new MemberImageSettings { BannerMaxBytes = limit });

        var result = await service.StoreAsync(MemberImageKind.Banner, ImageStream("png", limit + 1));

        result.Status.ShouldBe(MemberImageStatus.TooLarge);
        _storage.Files.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeletingAnImage_RemovesItsMetadataAndFile()
    {
        var stored = await _service.StoreAsync(MemberImageKind.ProfilePicture, ImageStream("png", 50));

        await _service.DeleteAsync(stored.AssetId!);

        _metadata.Assets.ShouldBeEmpty();
        _storage.Files.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeletingAnUnknownImage_DoesNothing()
    {
        await Should.NotThrowAsync(() => _service.DeleteAsync("nobody"));
    }

    [Fact]
    public async Task DeletingAnImage_WhenTheFileCannotBeDeleted_FailsAndKeepsEverythingForARetry()
    {
        var stored = await _service.StoreAsync(MemberImageKind.ProfilePicture, ImageStream("png", 50));
        _storage.FailDeletes = true;

        var exception = await Should.ThrowAsync<AppException>(() => _service.DeleteAsync(stored.AssetId!));

        exception.Error.Code.ShouldBe(ErrorCodes.AssetFileDeletionFailed);
        _metadata.Assets.ContainsKey(stored.AssetId!).ShouldBeTrue();
        _storage.Files.Count.ShouldBe(1);

        _storage.FailDeletes = false;
        await _service.DeleteAsync(stored.AssetId!);

        _metadata.Assets.ShouldBeEmpty();
        _storage.Files.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeletingAnImage_WhenTheMetadataCannotBeDeleted_LeavesNoOrphanFile_AndARetryFinishesTheJob()
    {
        var stored = await _service.StoreAsync(MemberImageKind.ProfilePicture, ImageStream("png", 50));
        _metadata.FailDeletes = true;

        var exception = await Should.ThrowAsync<AppException>(() => _service.DeleteAsync(stored.AssetId!));

        exception.Error.Code.ShouldBe(ErrorCodes.AssetMetadataDeletionFailed);
        _metadata.Assets.ContainsKey(stored.AssetId!).ShouldBeTrue();
        _storage.Files.ShouldBeEmpty();

        _metadata.FailDeletes = false;
        await _service.DeleteAsync(stored.AssetId!);

        _metadata.Assets.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeletingAnImage_WhoseFileIsAlreadyGone_StillRemovesTheMetadata()
    {
        var stored = await _service.StoreAsync(MemberImageKind.ProfilePicture, ImageStream("png", 50));
        var metadata = _metadata.Assets[stored.AssetId!];
        await _storage.DeleteAsync(metadata.type, metadata.FileRelativePath);

        await Should.NotThrowAsync(() => _service.DeleteAsync(stored.AssetId!));

        _metadata.Assets.ShouldBeEmpty();
    }

    #endregion

    #region Private Methods

    // A member image service over the in-memory fakes, with the given limits
    private MemberImageService CreateService(MemberImageSettings settings)
    {
        var assetService = new AssetService(_storage, _metadata, NullLogger<AssetService>.Instance);

        return new MemberImageService(assetService, Options.Create(settings));
    }

    // A stream of the given total size that starts like a real image of the given format
    private static MemoryStream ImageStream(string format, int size)
    {
        var header = format switch
        {
            "png" => new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 },
            "jpeg" => new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
            "webp" => new byte[] { 0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50 },
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };

        var bytes = new byte[size];
        header.CopyTo(bytes, 0);

        return new MemoryStream(bytes);
    }

    // Bytes of the given size that do not start like any accepted image
    private static byte[] NotAnImage(int size) => Enumerable.Repeat((byte)'x', size).ToArray();

    #endregion
}
