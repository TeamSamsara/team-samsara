// File: /team-samsara/apps/api/src/TeamSamsara.Modules.Assets/MemberImageService.cs
// Version: 1.0.0
// Latest commit: feature/identity-profile
// Author: Gerrah
//
// Purpose: Validates and stores member images through the asset pipeline, for other modules.

using Microsoft.Extensions.Options;
using TeamSamsara.Shared.Assets;

namespace TeamSamsara.Modules.Assets.Services;

public class MemberImageService : IMemberImageService
{
    #region Fields

    private const int ReadChunkSize = 81920;
    private const string BaseFileName = "member-image";
    private const string ProfilePictureAlt = "Profile picture";
    private const string BannerAlt = "Banner";

    private readonly AssetService _assetService;
    private readonly MemberImageSettings _settings;

    #endregion

    #region Constructors

    // Initializes the service with the asset pipeline it stores images through and its limits
    public MemberImageService(AssetService assetService, IOptions<MemberImageSettings> settings)
    {
        _assetService = assetService;
        _settings = settings.Value;
    }

    #endregion

    #region Public Methods

    // Reads the image up to its size limit, checks it is a PNG, JPEG or WebP, then stores it
    public async Task<MemberImageResult> StoreAsync(MemberImageKind kind, Stream content)
    {
        var bytes = await ReadWithinLimitAsync(content, MaxBytesFor(kind));

        if (bytes is null)
        {
            return new MemberImageResult(MemberImageStatus.TooLarge, null);
        }

        if (!MemberImageFormatDetector.TryDetect(bytes, out var contentType, out var extension))
        {
            return new MemberImageResult(MemberImageStatus.UnsupportedType, null);
        }

        await using var upload = new MemoryStream(bytes);

        var metadata = await _assetService.UploadAssetAsync(
            upload, BaseFileName + extension, contentType, AltTextFor(kind));

        return new MemberImageResult(MemberImageStatus.Success, metadata.Id);
    }

    // Deletes the stored image; a missing asset is not an error
    public async Task DeleteAsync(string assetId)
    {
        await _assetService.DeleteAssetAsync(assetId);
    }

    #endregion

    #region Private Methods

    // The largest accepted size for this kind of image
    private int MaxBytesFor(MemberImageKind kind) => kind switch
    {
        MemberImageKind.ProfilePicture => _settings.ProfilePictureMaxBytes,
        MemberImageKind.Banner => _settings.BannerMaxBytes,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    // The alt text stored with this kind of image
    private static string AltTextFor(MemberImageKind kind) => kind switch
    {
        MemberImageKind.ProfilePicture => ProfilePictureAlt,
        MemberImageKind.Banner => BannerAlt,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    // Reads the stream into memory, giving up (null) as soon as it passes the limit
    private static async Task<byte[]?> ReadWithinLimitAsync(Stream content, int maxBytes)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[ReadChunkSize];

        int read;

        while ((read = await content.ReadAsync(chunk.AsMemory())) > 0)
        {
            if (buffer.Length + read > maxBytes)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    #endregion
}
