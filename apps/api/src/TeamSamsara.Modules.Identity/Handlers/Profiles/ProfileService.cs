// File: /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/ProfileService.cs
// Version: 1.1.0
// Latest commit: fix/profile-atomic-update
// Author: Gerrah
//
// Purpose: Default IProfileService: validates profile text and keeps images and profile in step.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Repositories;
using TeamSamsara.Shared.Assets;

namespace TeamSamsara.Modules.Identity.Handlers;

public class ProfileService : IProfileService
{
    #region Fields

    private const char LineFeed = '\n';
    private const char CarriageReturn = '\r';

    private readonly IProfileStore _profiles;
    private readonly IMemberImageService _images;
    private readonly ProfileSettings _settings;
    private readonly ILogger<ProfileService> _logger;

    #endregion

    #region Constructors

    public ProfileService(
        IProfileStore profiles,
        IMemberImageService images,
        IOptions<ProfileSettings> settings,
        ILogger<ProfileService> logger)
    {
        _profiles = profiles;
        _images = images;
        _settings = settings.Value;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    // Returns the member's profile, or ProfileNotFound if they have none
    public async Task<ProfileResult> GetAsync(string userId)
    {
        var profile = await _profiles.GetByIdAsync(userId);

        return profile is null ? Failure(ProfileStatus.ProfileNotFound) : Success(profile);
    }

    // Replaces the display name and bio once both pass validation
    public async Task<ProfileResult> UpdateAsync(string userId, UpdateProfileRequest request)
    {
        var displayName = request.DisplayName?.Trim();

        if (!IsValidDisplayName(displayName))
        {
            return Failure(ProfileStatus.InvalidDisplayName);
        }

        var bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim();

        if (!IsValidBio(bio))
        {
            return Failure(ProfileStatus.InvalidBio);
        }

        var profile = await _profiles.ModifyAsync(userId, current =>
        {
            current.DisplayName = displayName;
            current.Bio = bio;
        });

        return profile is null ? Failure(ProfileStatus.ProfileNotFound) : Success(profile);
    }

    // Stores the image, points the profile at it, then removes the image it replaced
    public async Task<ProfileResult> SetImageAsync(string userId, MemberImageKind kind, Stream content)
    {
        // Checked first so an image is never stored for a member who has no profile
        if (await _profiles.GetByIdAsync(userId) is null)
        {
            return Failure(ProfileStatus.ProfileNotFound);
        }

        var stored = await _images.StoreAsync(kind, content);

        if (stored.Status != MemberImageStatus.Success)
        {
            return Failure(ToFailureStatus(stored.Status));
        }

        var newAssetId = stored.AssetId!;
        string? previousAssetId = null;
        Profile? profile;

        try
        {
            profile = await _profiles.ModifyAsync(userId, current =>
            {
                previousAssetId = GetImageId(current, kind);
                SetImageId(current, kind, newAssetId);
            });
        }
        catch
        {
            await TryDeleteImageAsync(newAssetId);
            throw;
        }

        // The profile was removed while the image was being stored
        if (profile is null)
        {
            await TryDeleteImageAsync(newAssetId);

            return Failure(ProfileStatus.ProfileNotFound);
        }

        if (previousAssetId is not null)
        {
            await TryDeleteImageAsync(previousAssetId);
        }

        return Success(profile);
    }

    // Unlinks the image from the profile, then removes it. Clearing an empty slot succeeds.
    public async Task<ProfileResult> ClearImageAsync(string userId, MemberImageKind kind)
    {
        string? previousAssetId = null;

        var profile = await _profiles.ModifyAsync(userId, current =>
        {
            previousAssetId = GetImageId(current, kind);
            SetImageId(current, kind, null);
        });

        if (profile is null)
        {
            return Failure(ProfileStatus.ProfileNotFound);
        }

        if (previousAssetId is not null)
        {
            await TryDeleteImageAsync(previousAssetId);
        }

        return Success(profile);
    }

    #endregion

    #region Private Methods

    // A name needs at least one visible character, must fit the limits, and has no control characters
    private bool IsValidDisplayName([NotNullWhen(true)] string? displayName)
    {
        if (string.IsNullOrEmpty(displayName))
        {
            return false;
        }

        var length = LengthOf(displayName);

        return length >= _settings.DisplayNameMinLength
            && length <= _settings.DisplayNameMaxLength
            && !displayName.Any(char.IsControl);
    }

    // No bio is valid; a bio must fit the limit and may only use line breaks as control characters
    private bool IsValidBio(string? bio)
    {
        if (bio is null)
        {
            return true;
        }

        return LengthOf(bio) <= _settings.BioMaxLength
            && !bio.Any(character => char.IsControl(character)
                && character != LineFeed
                && character != CarriageReturn);
    }

    // Counts what a reader sees as characters, so an emoji or an accented letter counts once
    private static int LengthOf(string text) => new StringInfo(text).LengthInTextElements;

    // The asset id currently held for this kind of image, or null
    private static string? GetImageId(Profile profile, MemberImageKind kind) => kind switch
    {
        MemberImageKind.ProfilePicture => profile.ProfilePictureAssetId,
        MemberImageKind.Banner => profile.BannerAssetId,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    // Points the profile's picture or banner at the given asset id (null clears it)
    private static void SetImageId(Profile profile, MemberImageKind kind, string? assetId)
    {
        switch (kind)
        {
            case MemberImageKind.ProfilePicture:
                profile.ProfilePictureAssetId = assetId;
                break;
            case MemberImageKind.Banner:
                profile.BannerAssetId = assetId;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    // Translates a refused image upload into a profile outcome
    private static ProfileStatus ToFailureStatus(MemberImageStatus status) => status switch
    {
        MemberImageStatus.UnsupportedType => ProfileStatus.ImageUnsupportedType,
        MemberImageStatus.TooLarge => ProfileStatus.ImageTooLarge,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Not a failed image result.")
    };

    // Removes an image that is no longer used; a failure is logged, not surfaced, since the
    // member's request has already succeeded
    private async Task TryDeleteImageAsync(string assetId)
    {
        try
        {
            await _images.DeleteAsync(assetId);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Could not delete the unused member image '{AssetId}'.", assetId);
        }
    }

    private static ProfileResult Success(Profile profile) => new(ProfileStatus.Success, ToView(profile));

    private static ProfileResult Failure(ProfileStatus status) => new(status, null);

    private static ProfileView ToView(Profile profile) => new(
        profile.DisplayName,
        profile.Bio,
        profile.ProfilePictureAssetId,
        profile.BannerAssetId);

    #endregion
}
