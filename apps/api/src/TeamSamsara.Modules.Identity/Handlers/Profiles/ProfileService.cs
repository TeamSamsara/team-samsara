// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/Profiles/ProfileService.cs
// Version : 1.2.0
// Latest commit: task/api-profile-onboarding
// Author : Gerrah
// Purpose : Default IProfileService: validates profile text and handles, and keeps images,
// handle and profile in step.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
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

    private const char LineFeed = '\n';
    private const char CarriageReturn = '\r';
    private const char HandleSeparator = '_';
    private const string NeutralHandle = "member";
    private const int SuffixDigits = 4;
    private const int SuffixAttempts = 8;
    private const int SuffixMin = 1000;
    private const int SuffixMax = 10000;
    private const int LastResortIdLength = 8;

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

    // Gives the member a handle made from their display name, trying a few numbered variants
    // when it is taken. Does nothing if they already have one or have no profile.
    public async Task EnsureHandleAsync(string userId)
    {
        var profile = await _profiles.GetByIdAsync(userId);

        if (profile is null || profile.Handle is not null)
        {
            return;
        }

        foreach (var candidate in CandidateHandles(profile.DisplayName))
        {
            var status = await _handles.ClaimAsync(userId, candidate, null);

            if (status != HandleClaimStatus.HandleTaken)
            {
                return;
            }
        }

        throw new InvalidOperationException($"No free handle could be found for member '{userId}'.");
    }

    // Tells whether the member could take this handle, without changing anything
    public async Task<ProfileStatus> CheckHandleAsync(string userId, string handle)
    {
        var normalized = NormalizeHandle(handle);

        if (!IsValidHandle(normalized))
        {
            return ProfileStatus.InvalidHandle;
        }

        if (IsForbiddenHandle(normalized))
        {
            return ProfileStatus.HandleTaken;
        }

        var owner = await _handles.GetOwnerAsync(normalized);

        return owner is null || owner == userId ? ProfileStatus.Success : ProfileStatus.HandleTaken;
    }

    // Changes the handle. Keeping the current handle succeeds without starting the cooldown.
    public async Task<ProfileResult> SetHandleAsync(string userId, SetHandleRequest request)
    {
        var handle = NormalizeHandle(request.Handle);

        if (!IsValidHandle(handle))
        {
            return Failure(ProfileStatus.InvalidHandle);
        }

        if (IsForbiddenHandle(handle))
        {
            return Failure(ProfileStatus.HandleTaken);
        }

        var profile = await _profiles.GetByIdAsync(userId);

        if (profile is null)
        {
            return Failure(ProfileStatus.ProfileNotFound);
        }

        if (profile.Handle == handle)
        {
            return Success(profile);
        }

        if (IsCoolingDown(profile))
        {
            return Failure(ProfileStatus.HandleCooldown);
        }

        var claim = await _handles.ClaimAsync(userId, handle, _clock.UtcNow);

        return claim switch
        {
            HandleClaimStatus.Success => await GetAsync(userId),
            HandleClaimStatus.HandleTaken => Failure(ProfileStatus.HandleTaken),
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
        var normalized = NormalizeHandle(handle);

        if (!IsValidHandle(normalized))
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

    // A name needs at least one visible character, must fit the limits, has no control
    // characters and no blocked word
    private bool IsValidDisplayName([NotNullWhen(true)] string? displayName)
    {
        if (string.IsNullOrEmpty(displayName))
        {
            return false;
        }

        var length = LengthOf(displayName);

        return length >= _settings.DisplayNameMinLength
            && length <= _settings.DisplayNameMaxLength
            && !displayName.Any(char.IsControl)
            && !BlockedWords.IsBlockedName(displayName);
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

    // The handle as the member typed it, lowercased and without a leading @
    private static string NormalizeHandle(string? handle)
    {
        return handle?.Trim().TrimStart('@').ToLowerInvariant() ?? string.Empty;
    }

    // A handle fits the length limits, uses only a-z, 0-9 and _, and does not start or end with _
    private bool IsValidHandle(string handle)
    {
        return handle.Length >= _settings.HandleMinLength
            && handle.Length <= _settings.HandleMaxLength
            && handle.All(IsHandleCharacter)
            && handle[0] != HandleSeparator
            && handle[^1] != HandleSeparator;
    }

    private static bool IsHandleCharacter(char character)
    {
        return character is >= 'a' and <= 'z' or >= '0' and <= '9' or HandleSeparator;
    }

    // Reserved and blocked handles are refused the same way a taken one is
    private static bool IsForbiddenHandle(string handle)
    {
        return ReservedHandles.IsReserved(handle) || BlockedWords.IsBlockedHandle(handle);
    }

    // True while the member has changed their handle more recently than the cooldown allows
    private bool IsCoolingDown(Profile profile)
    {
        return _settings.HandleChangeCooldownDays > 0
            && profile.HandleChangedAt is { } changedAt
            && _clock.UtcNow < changedAt.AddDays(_settings.HandleChangeCooldownDays);
    }

    // The handles to try for a new member, best first: the display name itself, then numbered
    // variants of it. A name that cannot be used as a handle, such as the neutral "Member",
    // is replaced by the neutral handle.
    private IEnumerable<string> CandidateHandles(string displayName)
    {
        var seed = SeedFrom(displayName);
        var usable = seed.Length >= _settings.HandleMinLength && !IsForbiddenHandle(seed);
        var stem = usable ? seed : NeutralHandle;

        if (usable)
        {
            yield return seed;
        }

        var stemLength = Math.Min(stem.Length, _settings.HandleMaxLength - SuffixDigits);

        for (var attempt = 0; attempt < SuffixAttempts; attempt++)
        {
            var number = Random.Shared.Next(SuffixMin, SuffixMax).ToString(CultureInfo.InvariantCulture);

            yield return stem[..stemLength] + number;
        }

        var id = Guid.NewGuid().ToString("N")[..LastResortIdLength];

        yield return NeutralHandle + id[..Math.Min(LastResortIdLength, _settings.HandleMaxLength - NeutralHandle.Length)];
    }

    // The display name reduced to lowercase letters and digits, cut to the handle length
    private string SeedFrom(string displayName)
    {
        var builder = new StringBuilder();

        foreach (var character in displayName.ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            if (character is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                builder.Append(character);
            }
        }

        var seed = builder.ToString();

        return seed.Length > _settings.HandleMaxLength ? seed[.._settings.HandleMaxLength] : seed;
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
        profile.Handle,
        profile.Bio,
        profile.ProfilePictureAssetId,
        profile.BannerAssetId,
        profile.OnboardingCompleted);

    #endregion
}
