// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/Profiles/ProfileService.cs
// Version : 1.4.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : Default IProfileService: keeps images, handle and profile in step. The text,
// handle and cooldown rules live in ProfileText, HandleFormat, HandleCandidates and the handle store.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Repositories;
using TeamSamsara.Shared.Assets;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Handlers;

public class ProfileService : IProfileService
{
    #region Fields

    private readonly IProfileStore _profiles;
    private readonly IHandleStore _handles;
    private readonly IMemberImageService _images;
    private readonly ProfileSettings _settings;
    private readonly IClock _clock;
    private readonly ILogger<ProfileService> _logger;

    #endregion

    #region Constructors

    public ProfileService(
        IProfileStore profiles,
        IHandleStore handles,
        IMemberImageService images,
        IOptions<ProfileSettings> settings,
        IClock clock,
        ILogger<ProfileService> logger)
    {
        _profiles = profiles;
        _handles = handles;
        _images = images;
        _settings = settings.Value;
        _clock = clock;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    // Returns the member's profile, or ProfileNotFound if they have none. A profile without a
    // handle is given one first, which is how profiles made before handles existed catch up.
    public async Task<ProfileResult> GetAsync(string userId)
    {
        var profile = await _profiles.GetByIdAsync(userId);

        if (profile is null)
        {
            return Failure(ProfileStatus.ProfileNotFound);
        }

        if (profile.Handle is null)
        {
            await EnsureHandleAsync(userId);
            profile = await _profiles.GetByIdAsync(userId) ?? profile;
        }

        return Success(profile);
    }

    // Replaces the display name and bio once both pass validation
    public async Task<ProfileResult> UpdateAsync(string userId, UpdateProfileRequest request)
    {
        var displayName = request.DisplayName?.Trim();

        if (!ProfileText.IsValidDisplayName(displayName, _settings))
        {
            return Failure(ProfileStatus.InvalidDisplayName);
        }

        var bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim();

        if (!ProfileText.IsValidBio(bio, _settings))
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

        return await LinkImageAsync(userId, kind, stored.AssetId!);
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

    // Gives the member a handle made from their display name, trying a few numbered variants
    // when it is taken. Does nothing if they already have one or have no profile.
    public async Task EnsureHandleAsync(string userId)
    {
        var profile = await _profiles.GetByIdAsync(userId);

        if (profile is null || profile.Handle is not null)
        {
            return;
        }

        foreach (var candidate in HandleCandidates.For(profile.DisplayName, _settings))
        {
            var claim = await _handles.ClaimAsync(userId, candidate, null);

            if (claim.Status != HandleClaimStatus.HandleTaken)
            {
                return;
            }
        }

        throw new InvalidOperationException($"No free handle could be found for member '{userId}'.");
    }

    // Tells whether the member could take this handle, without changing anything
    public async Task<ProfileStatus> CheckHandleAsync(string userId, string handle)
    {
        if (!HandleFormat.TryNormalize(handle, _settings, out var normalized, out var refusal))
        {
            return refusal;
        }

        var owner = await _handles.GetOwnerAsync(normalized);

        return owner is null || owner == userId ? ProfileStatus.Success : ProfileStatus.HandleTaken;
    }

    // Changes the handle. Keeping the current handle succeeds without starting the cooldown,
    // and so does choosing one while the member is still setting up their account. The
    // handle store enforces the cooldown where it reads the profile.
    public async Task<ProfileResult> SetHandleAsync(string userId, SetHandleRequest request)
    {
        if (!HandleFormat.TryNormalize(request.Handle, _settings, out var handle, out var refusal))
        {
            return Failure(refusal);
        }

        var change = new HandleChange(_clock.UtcNow, _settings.HandleChangeCooldownDays);
        var claim = await _handles.ClaimAsync(userId, handle, change);

        return claim.Status switch
        {
            HandleClaimStatus.Success => Success(claim.Profile!),
            HandleClaimStatus.HandleTaken => Failure(ProfileStatus.HandleTaken),
            HandleClaimStatus.HandleCooldown => Failure(ProfileStatus.HandleCooldown),
            _ => Failure(ProfileStatus.ProfileNotFound)
        };
    }

    // Marks onboarding as finished. Doing it again changes nothing.
    public async Task<ProfileResult> CompleteOnboardingAsync(string userId)
    {
        var profile = await _profiles.ModifyAsync(userId, current => current.OnboardingCompleted = true);

        return profile is null ? Failure(ProfileStatus.ProfileNotFound) : Success(profile);
    }

    // Returns what a visitor sees of the member who holds the handle, or null if nobody does
    public async Task<PublicProfileView?> GetPublicAsync(string handle)
    {
        var normalized = HandleFormat.Normalize(handle);

        if (!HandleFormat.IsValid(normalized, _settings))
        {
            return null;
        }

        var owner = await _handles.GetOwnerAsync(normalized);

        if (owner is null)
        {
            return null;
        }

        var profile = await _profiles.GetByIdAsync(owner);

        return profile is null
            ? null
            : new PublicProfileView(
                profile.DisplayName,
                normalized,
                profile.Bio,
                profile.ProfilePictureAssetId,
                profile.BannerAssetId);
    }

    #endregion

    #region Private Methods

    // Points the profile at the stored image, then removes the image it replaced
    private async Task<ProfileResult> LinkImageAsync(string userId, MemberImageKind kind, string newAssetId)
    {
        var (profile, previousAssetId) = await ReplaceImageAsync(userId, kind, newAssetId);

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

    // Swaps the new image into the profile and returns the image it replaced. The new image
    // is removed again if the swap fails or the profile was removed while it was being stored.
    private async Task<(Profile? Profile, string? PreviousAssetId)> ReplaceImageAsync(
        string userId,
        MemberImageKind kind,
        string newAssetId)
    {
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

        if (profile is null)
        {
            await TryDeleteImageAsync(newAssetId);
        }

        return (profile, previousAssetId);
    }

    // True while the member has changed their handle more recently than the cooldown allows
    private bool IsCoolingDown(Profile profile)
    {
        return _settings.HandleChangeCooldownDays > 0
            && profile.HandleChangedAt is { } changedAt
            && _clock.UtcNow < changedAt.AddDays(_settings.HandleChangeCooldownDays);
    }

    // When the member can change their handle again, or null when they can do it now
    private DateTimeOffset? GetHandleChangeableAt(Profile profile)
    {
        return IsCoolingDown(profile)
            ? profile.HandleChangedAt?.AddDays(_settings.HandleChangeCooldownDays)
            : null;
    }

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

    private ProfileResult Success(Profile profile) => new(ProfileStatus.Success, ToView(profile));

    private static ProfileResult Failure(ProfileStatus status) => new(status, null);

    private ProfileView ToView(Profile profile) => new(
        profile.DisplayName,
        profile.Handle,
        profile.Bio,
        profile.ProfilePictureAssetId,
        profile.BannerAssetId,
        profile.OnboardingCompleted,
        GetHandleChangeableAt(profile));

    #endregion
}
