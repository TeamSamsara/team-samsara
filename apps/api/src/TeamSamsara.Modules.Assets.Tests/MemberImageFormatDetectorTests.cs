// File: /team-samsara/apps/api/src/TeamSamsara.Modules.Assets.Tests/MemberImageFormatDetectorTests.cs
// Version: 1.0.0
// Latest commit: feature/identity-profile
// Author: Gerrah
//
// Purpose: Proves the detector recognizes PNG, JPEG and WebP from their first bytes and rejects the rest.

using System.Text;
using Shouldly;
using TeamSamsara.Modules.Assets.Services;

namespace TeamSamsara.Modules.Assets.Tests;

public class MemberImageFormatDetectorTests
{
    #region Public Methods

    [Fact]
    public void APng_IsRecognized()
    {
        var detected = MemberImageFormatDetector.TryDetect(PngBytes(), out var contentType, out var extension);

        detected.ShouldBeTrue();
        contentType.ShouldBe("image/png");
        extension.ShouldBe(".png");
    }

    [Fact]
    public void AJpeg_IsRecognized()
    {
        var detected = MemberImageFormatDetector.TryDetect(JpegBytes(), out var contentType, out var extension);

        detected.ShouldBeTrue();
        contentType.ShouldBe("image/jpeg");
        extension.ShouldBe(".jpg");
    }

    [Fact]
    public void AWebp_IsRecognized_EvenAtItsMinimumLength()
    {
        var detected = MemberImageFormatDetector.TryDetect(WebpBytes(), out var contentType, out var extension);

        detected.ShouldBeTrue();
        contentType.ShouldBe("image/webp");
        extension.ShouldBe(".webp");
    }

    [Theory]
    [InlineData("")]
    [InlineData("GIF89a")]
    [InlineData("just some text")]
    [InlineData("RIFF....WAVE")]
    [InlineData("RIFF")]
    public void AnythingElse_IsNotRecognized(string content)
    {
        var detected = MemberImageFormatDetector.TryDetect(
            Encoding.ASCII.GetBytes(content), out var contentType, out var extension);

        detected.ShouldBeFalse();
        contentType.ShouldBeEmpty();
        extension.ShouldBeEmpty();
    }

    #endregion

    #region Private Methods

    // The PNG signature followed by a few filler bytes
    private static byte[] PngBytes() => new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0 };

    // The JPEG start marker followed by a few filler bytes
    private static byte[] JpegBytes() => new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0 };

    // "RIFF", a 4-byte size, then "WEBP": the shortest recognizable WebP
    private static byte[] WebpBytes() => new byte[] { 0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50 };

    #endregion
}
