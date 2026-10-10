// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/PasswordResetService.cs
// Version : 1.0.0
// Latest commit: feat/password-reset-service
// Author : Gerrah
// Purpose : Password reset for signed-out members: reset code, one-time token, new password.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Repositories;
using TeamSamsara.Shared.Alert;
using TeamSamsara.Shared.BackgroundTasks;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Handlers;

public class PasswordResetService : IPasswordResetService
{
    #region Fields

    private readonly IBackgroundTaskQueue _queue;
    private readonly IGuardDog _guardDog;
    private readonly IVerificationCodeService _verification;
    private readonly IPasswordResetTokenStore _tokens;
    private readonly IUserStore _users;
    private readonly IAccountGateway _accounts;
    private readonly ISignInTracker _signIns;
    private readonly IAlertSender _alerts;
    private readonly IClock _clock;
    private readonly VerificationSettings _verificationSettings;
    private readonly PasswordSettings _passwordSettings;
    private readonly ILogger<PasswordResetService> _logger;

    #endregion

    #region Constructors

    // Initializes the service with its stores, queue, notification channel and settings.
    public PasswordResetService(
        IBackgroundTaskQueue queue,
        IGuardDog guardDog,
        IVerificationCodeService verification,
        IPasswordResetTokenStore tokens,
        IUserStore users,
        IAccountGateway accounts,
        ISignInTracker signIns,
        IAlertSender alerts,
        IClock clock,
        IOptions<VerificationSettings> verificationSettings,
        IOptions<PasswordSettings> passwordSettings,
        ILogger<PasswordResetService> logger)
    {
        _queue = queue;
        _guardDog = guardDog;
        _verification = verification;
        _tokens = tokens;
        _users = users;
        _accounts = accounts;
        _signIns = signIns;
        _alerts = alerts;
        _clock = clock;
        _verificationSettings = verificationSettings.Value;
        _passwordSettings = passwordSettings.Value;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    // Queues the code delivery and returns the same answer for every address.
    public Task<PasswordCodeResult> RequestResetAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        QueueDelivery(email);

        return Task.FromResult(BuildRequestAnswer());
    }

    // Verifies the reset code and returns a one-time reset token.
    public async Task<PasswordResetTokenResult> VerifyCodeAsync(
        string email,
        string code,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
        {
            return Failed(PasswordStatus.InvalidCode);
        }

        var address = email.Trim();
        var verification = await _verification.VerifyAsync(
            PasswordResetCrypto.HashEmail(address),
            VerificationPurpose.PasswordReset,
            code,
            cancellationToken);

        if (verification != VerificationResult.Valid)
        {
            return Failed(ToFailureStatus(verification));
        }

        var userId = await _accounts.GetUserIdByEmailAsync(address);

        if (userId is null)
        {
            return Failed(PasswordStatus.AccountNotFound);
        }

        return new PasswordResetTokenResult(PasswordStatus.Success, await IssueTokenAsync(userId));
    }

    // Sets a new password using a reset token and signs the member out everywhere.
    public async Task<PasswordStatus> SetNewPasswordAsync(
        string resetToken,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        // Checked first so a weak password does not consume the token.
        if (!MeetsRules(newPassword))
        {
            return PasswordStatus.InvalidPassword;
        }

        var token = await TakeValidTokenAsync(resetToken);

        if (token is null)
        {
            return PasswordStatus.InvalidResetToken;
        }

        if (!await IsMemberAsync(token.UserId))
        {
            return PasswordStatus.AccountNotFound;
        }

        await ApplyNewPasswordAsync(token.UserId, newPassword, cancellationToken);

        return PasswordStatus.Success;
    }

    #endregion

    #region Private Methods

    // The fixed answer returned for every reset request.
    private PasswordCodeResult BuildRequestAnswer()
    {
        return new PasswordCodeResult(
            PasswordStatus.Success,
            _verificationSettings.PasswordResetCodeLength,
            _verificationSettings.ResendCooldownSeconds);
    }

    // Hands the lookup and email to the background queue.
    private void QueueDelivery(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var address = email.Trim();
        var client = _guardDog.Inspect();

        var queued = _queue.TryEnqueue((services, cancellationToken) =>
            services.GetRequiredService<IPasswordResetDelivery>()
                .DeliverAsync(address, client, cancellationToken));

        if (!queued)
        {
            _logger.LogWarning("The background queue is full; a password reset request was dropped.");
        }
    }

    // Creates and stores a reset token; only its hash is persisted.
    private async Task<string> IssueTokenAsync(string userId)
    {
        var token = PasswordResetCrypto.GenerateToken();
        var now = _clock.UtcNow;

        await _tokens.SaveAsync(new PasswordResetToken
        {
            Id = PasswordResetCrypto.HashToken(token),
            UserId = userId,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(_passwordSettings.ResetTokenMinutes)
        });

        return token;
    }

    // Consumes the token; null if it is blank, unknown, already used or expired.
    private async Task<PasswordResetToken?> TakeValidTokenAsync(string resetToken)
    {
        if (string.IsNullOrWhiteSpace(resetToken))
        {
            return null;
        }

        var token = await _tokens.TakeAsync(PasswordResetCrypto.HashToken(resetToken.Trim()));

        return token is not null && _clock.UtcNow < token.ExpiresAt ? token : null;
    }

    // Accounts pending deletion still qualify; a reset does not restore them.
    private async Task<bool> IsMemberAsync(string userId)
    {
        var user = await _users.GetByIdAsync(userId);

        return user is { AccessLevel: AccessLevel.Member };
    }

    // Replaces the password, ends every session and notifies the member.
    private async Task ApplyNewPasswordAsync(
        string userId,
        string newPassword,
        CancellationToken cancellationToken)
    {
        await _accounts.SetPasswordAsync(userId, newPassword);
        await _accounts.RevokeSessionsAsync(userId);
        await _signIns.ClearVerifiedAsync(userId);
        await NotifyPasswordChangedAsync(userId, cancellationToken);
    }

    // Best effort: a failed notice must not fail a completed reset.
    private async Task NotifyPasswordChangedAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            await SendPasswordChangedNoticeAsync(userId, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Could not send the password-changed notice.");
        }
    }

    // Emails the password-changed notice to the account's address.
    private async Task SendPasswordChangedNoticeAsync(string userId, CancellationToken cancellationToken)
    {
        var email = await _accounts.GetEmailAsync(userId);

        if (email is null)
        {
            return;
        }

        await _alerts.SendAlertAsync(
            email,
            AlertType.PasswordChanged,
            new Dictionary<string, string>(),
            cancellationToken);
    }

    // Validates the password length against the configured limits.
    private bool MeetsRules(string? password)
    {
        return password is not null
            && password.Length >= _passwordSettings.MinLength
            && password.Length <= _passwordSettings.MaxLength;
    }

    // A failed verification result with no token.
    private static PasswordResetTokenResult Failed(PasswordStatus status)
    {
        return new PasswordResetTokenResult(status, null);
    }

    // Maps a failed code check to a status; a missing code reads as invalid so unknown addresses look the same.
    private static PasswordStatus ToFailureStatus(VerificationResult verification)
    {
        return verification switch
        {
            VerificationResult.Invalid => PasswordStatus.InvalidCode,
            VerificationResult.NotFound => PasswordStatus.InvalidCode,
            VerificationResult.Expired => PasswordStatus.CodeExpired,
            VerificationResult.TooManyAttempts => PasswordStatus.TooManyAttempts,
            _ => throw new ArgumentOutOfRangeException(
                nameof(verification), verification, "Not a failed verification result.")
        };
    }

    #endregion
}
