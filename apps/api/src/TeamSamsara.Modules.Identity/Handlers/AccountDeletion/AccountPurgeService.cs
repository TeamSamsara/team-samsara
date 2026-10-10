// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/AccountDeletion/AccountPurgeService.cs
// Version : 1.0.0
// Latest commit: feat/account-purge
// Author : Gerrah
// Purpose : Default IAccountPurgeService. Each account is claimed with a lease, then removed in an order whose every step can be repeated, with the user record last so a half-finished purge is found and finished by the next run.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Repositories;
using TeamSamsara.Shared.Assets;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Handlers;

public class AccountPurgeService : IAccountPurgeService
{
    #region Fields

    private readonly IUserStore _users;
    private readonly IProfileStore _profiles;
    private readonly IVerificationCodeStore _codes;
    private readonly IPasswordResetTokenStore _resetTokens;
    private readonly IEmailChangeRequestStore _emailChanges;
    private readonly IAccountGateway _accounts;
    private readonly IMemberImageService _images;
    private readonly IClock _clock;
    private readonly AuthenticationSettings _authenticationSettings;
    private readonly AccountPurgeSettings _purgeSettings;
    private readonly ILogger<AccountPurgeService> _logger;

    #endregion

    #region Constructors

    public AccountPurgeService(
        IUserStore users,
        IProfileStore profiles,
        IVerificationCodeStore codes,
        IPasswordResetTokenStore resetTokens,
        IEmailChangeRequestStore emailChanges,
        IAccountGateway accounts,
        IMemberImageService images,
        IClock clock,
        IOptions<AuthenticationSettings> authenticationSettings,
        IOptions<AccountPurgeSettings> purgeSettings,
        ILogger<AccountPurgeService> logger)
    {
        _users = users;
        _profiles = profiles;
        _codes = codes;
        _resetTokens = resetTokens;
        _emailChanges = emailChanges;
        _accounts = accounts;
        _images = images;
        _clock = clock;
        _authenticationSettings = authenticationSettings.Value;
        _purgeSettings = purgeSettings.Value;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    // Purges every expired deleted account it can claim; a failure on one account is counted and never stops the rest
    public async Task<AccountPurgeResult> PurgeExpiredAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = _clock.UtcNow.AddDays(-_authenticationSettings.RecoveryWindowDays);
        var expired = await _users.ListDeletedBeforeAsync(cutoff);

        var purged = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var user in expired)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (await TryPurgeAsync(user.Id, cutoff))
            {
                case PurgeOutcome.Purged:
                    purged++;
                    break;
                case PurgeOutcome.Skipped:
                    skipped++;
                    break;
                default:
                    failed++;
                    break;
            }
        }

        return new AccountPurgeResult(purged, skipped, failed);
    }

    #endregion

    #region Private Methods

    // Claims one account and removes it; Skipped when it was restored or another instance holds it, Failed on any error
    private async Task<PurgeOutcome> TryPurgeAsync(string userId, DateTimeOffset cutoff)
    {
        try
        {
            var now = _clock.UtcNow;
            var leaseUntil = now.AddMinutes(_purgeSettings.LeaseMinutes);

            if (!await _users.TryClaimForPurgeAsync(userId, cutoff, now, leaseUntil))
            {
                return PurgeOutcome.Skipped;
            }

            await RemoveAccountAsync(userId);

            _logger.LogInformation("Purged deleted account '{UserId}'.", userId);

            return PurgeOutcome.Purged;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Could not purge deleted account '{UserId}'; it will be retried.", userId);

            return PurgeOutcome.Failed;
        }
    }

    // Removes everything of the account; the user record goes last because it is how the next run finds a half-finished purge
    private async Task RemoveAccountAsync(string userId)
    {
        await RemoveProfileAsync(userId);
        await RemoveLeftoverRecordsAsync(userId);
        await _accounts.DeleteAsync(userId);
        await _users.DeleteAsync(userId);
    }

    // Deletes the profile images, then the profile, so a retry can still find the image ids it needs
    private async Task RemoveProfileAsync(string userId)
    {
        var profile = await _profiles.GetByIdAsync(userId);

        if (profile is null)
        {
            return;
        }

        await DeleteImageAsync(profile.ProfilePictureAssetId);
        await DeleteImageAsync(profile.BannerAssetId);
        await _profiles.DeleteAsync(userId);
    }

    // Deletes one profile image if the profile has it
    private async Task DeleteImageAsync(string? assetId)
    {
        if (assetId is not null)
        {
            await _images.DeleteAsync(assetId);
        }
    }

    // Deletes the pending codes, reset tokens and email change request instead of waiting for their expiry
    private async Task RemoveLeftoverRecordsAsync(string userId)
    {
        foreach (var purpose in Enum.GetValues<VerificationPurpose>())
        {
            await _codes.DeleteAsync(userId, purpose);
        }

        await _resetTokens.DeleteForUserAsync(userId);
        await _emailChanges.DeleteAsync(userId);
    }

    #endregion

    #region Nested Types

    // How one account fared in a purge run
    private enum PurgeOutcome
    {
        Purged,
        Skipped,
        Failed
    }

    #endregion
}
