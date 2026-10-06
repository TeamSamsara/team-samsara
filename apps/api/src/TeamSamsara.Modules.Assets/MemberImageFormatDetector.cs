// File: /team-samsara/apps/api/src/TeamSamsara.Modules.Assets/MemberImageFormatDetector.cs
// Version: 1.0.0
// Latest commit: feature/identity-profile
// Author: Gerrah
//
// Purpose: Recognizes PNG, JPEG and WebP images from their first bytes.

namespace TeamSamsara.Modules.Assets.Services;

public static class MemberImageFormatDetector
{
    #region Fields

    private const string PngContentType = "image/png";
    private const string JpegContentType = "image/jpeg";
    private const string WebpContentType = "image/webp";

    private const string PngExtension = ".png";
    private const string JpegExtension = ".jpg";
    private const string WebpExtension = ".webp";

    private const int WebpMarkerOffset = 8;
    private const int WebpMinimumLength = 12;

    private static readonly byte[] _pngSignature = { 137, 80, 78, 71, 13, 10, 26, 10 };
    private static readonly byte[] _jpegSignature = { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] _riffSignature = { 0x52, 0x49, 0x46, 0x46 };
    private static readonly byte[] _webpSignature = { 0x57, 0x45, 0x42, 0x50 };

    #endregion

    #region Public Methods

    // Reports the content type and file extension of a recognized PNG, JPEG or WebP image.
    // Returns false for anything else, including empty or truncated files.
    public static bool TryDetect(byte[] bytes, out string contentType, out string extension)
    {
        if (bytes.AsSpan().StartsWith(_pngSignature))
        {
            contentType = PngContentType;
            extension = PngExtension;
            return true;
        }

        if (bytes.AsSpan().StartsWith(_jpegSignature))
        {
            contentType = JpegContentType;
            extension = JpegExtension;
            return true;
        }

        if (IsWebp(bytes))
        {
            contentType = WebpContentType;
            extension = WebpExtension;
            return true;
        }

        contentType = string.Empty;
        extension = string.Empty;
        return false;
    }

    #endregion

    #region Private Methods

    // A WebP file is a RIFF container: "RIFF", a 4-byte size, then "WEBP"
    private static bool IsWebp(byte[] bytes)
    {
        return bytes.Length >= WebpMinimumLength
            && bytes.AsSpan().StartsWith(_riffSignature)
            && bytes.AsSpan(WebpMarkerOffset, _webpSignature.Length).SequenceEqual(_webpSignature);
    }

    #endregion
}
