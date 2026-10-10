// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/AccountDeletion/AccountDeletionService.cs
// Version : 1.0.0
// Latest commit: feat/account-deletion
// Author : Gerrah
// Purpose : Default IAccountDeletionService. Deleting commits the deletion marker and the wipe of
// verified sign-ins in one write, so the account stops counting as Member at once; session
// revocation and the notice come after and cannot undo it.

using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Repositories;
using TeamSamsara.Shared.Alert;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Handlers;

public class AccountDeletionService : IAccountDeletionService
{
    #region Fields

    private readonly IUserStore _users;
    private readonly IAccountGateway _accounts;
    private readonly ISignInTracker _signIns;
    private readonly IVerificationCodeService _verification;
    private readonly IAlertSender _alerts;
    private readonly IClock _clock;
    private readonly AuthenticationSettings _settings;
    private readonly ILogger<AccountDeletionService> _logger;

    #endregion

    #region Constructors

    public AccountDeletionService(
        IUserStore users,
        IAccountGateway accounts,
        ISignInTracker signIns,
        IVerificationCodeService verification,
        IAlertSender alerts,
        IClock clock,
        IOptions<AuthenticationSettings> settings,
        ILogger<AccountDeletionService> logger)
    {
        _users = users;
        _accounts = accounts;
        _signIns = signIns;
        _verification = verification;
        _alerts = alerts;
        _clock = clock;
        _settings = settings.Value;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    // Sends the confirmation code to the member's email
    public async Task<AccountDeletionCodeResult> RequestDeletionCodeAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var email = await FindActiveMemberEmailAsync(userId);

        if (email is null)
        {
            return Rejected(AccountDeletionStatus.AccountNotFound);
        }

        var issue = await _verification.IssueAsync(
            userId, VerificationPurpose.StepUp, email, cancellationToken);

        var status = issue.Status == VerificationIssueStatus.Sent
            ? AccountDeletionStatus.Success
            : AccountDeletionStatus.CooldownActive;

        return new AccountDeletionCodeResult(status, issue.CodeLength, issue.RetryAfterSeconds);
    }

    // Checks the code, commits the deletion, then ends every session and sends the notice
    public async Task<AccountDeletionStatus> DeleteAsync(
        string userId,
        string code,
        CancellationToken cancellationToken = default)
    {
        var email = await FindActiveMemberEmailAsync(userId);

        if (email is null)
        {
            return AccountDeletionStatus.AccountNotFound;
        }

        var verification = await _verification.VerifyAsync(
            userId, VerificationPurpose.StepUp, code, cancellationToken);

        if (verification != VerificationResult.Valid)
        {
            return ToFailureStatus(verification);
        }

        if (!await TryCommitDeletionAsync(userId))
        {
            return AccountDeletionStatus.NoPendingCode;
        }

        await ForgetSignInsAsync(userId);
        await RevokeSessionsAsync(userId);
        await NotifyDeletedAsync(email, cancellationToken);

        return AccountDeletionStatus.Success;
    }

    #endregion

    #region Private Methods

    // The email of a completed member whose account is not deleted, or null
    private async Task<string?> FindActiveMemberEmailAsync(string userId)
    {
        var user = await _users.GetByIdAsync(userId);

        if (user is null || user.AccessLevel != AccessLevel.Member || user.DeletedAt is not null)
        {
            return null;
        }

        return await _accounts.GetEmailAsync(userId);
    }

    // Marks the account deleted and wipes its verified sign-ins in one write; false if another request already did
    private async Task<bool> TryCommitDeletionAsync(string userId)
    {
        var committed = false;

        await _users.ModifyAsync(userId, user =>
        {
            committed = false;

            if (user.DeletedAt is not null)
            {
                return;
            }

            user.DeletedAt = _clock.UtcNow;
            user.VerifiedSignIns.Clear();
            committed = true;
        });

        return committed;
    }

    // Drops the cached sign-ins; a failure is logged because the committed deletion already blocks the account
    private async Task ForgetSignInsAsync(string userId)
    {
        try
        {
            await _signIns.ClearVerifiedAsync(userId);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Could not clear the cached sign-ins of deleted account '{UserId}'.", userId);
        }
    }

    // Revokes the refresh tokens; a failure is logged because the committed deletion already blocks the account
    private async Task RevokeSessionsAsync(string userId)
    {
        try
        {
            await _accounts.RevokeSessionsAsync(userId);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Could not revoke the sessions of deleted account '{UserId}'.", userId);
        }
    }

    // Tells the member the account was deleted; the deletion is committed, so a failure is only logged
    private async Task NotifyDeletedAsync(string email, CancellationToken cancellationToken)
    {
        var templateData = new Dictionary<string, string>
        {
            [AlertTemplateKeys.RecoveryDays] =
                _settings.RecoveryWindowDays.ToString(CultureInfo.InvariantCulture)
        };

        try
        {
            await _alerts.SendAlertAsync(email, AlertType.AccountDeleted, templateData, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Could not send the account-deleted notice.");
        }
    }

    // Translates a failed code check into a deletion outcome
    private static AccountDeletionStatus ToFailureStatus(VerificationResult verification)
    {
        return verification switch
        {
            VerificationResult.Invalid => AccountDeletionStatus.InvalidCode,
            VerificationResult.Expired => AccountDeletionStatus.CodeExpired,
            VerificationResult.TooManyAttempts => AccountDeletionStatus.TooManyAttempts,
            VerificationResult.NotFound => AccountDeletionStatus.NoPendingCode,
            _ => throw new ArgumentOutOfRangeException(
                nameof(verification), verification, "Not a failed verification result.")
        };
    }

    // A code request answer that carries no code details
    private static AccountDeletionCodeResult Rejected(AccountDeletionStatus status)
    {
        return new AccountDeletionCodeResult(status, 0, 0);
    }

    #endregion
}
