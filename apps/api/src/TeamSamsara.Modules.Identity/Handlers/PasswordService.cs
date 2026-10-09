// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/PasswordService.cs
// Version : 1.0.0
// Latest commit: feat/password-change-service
// Author : Gerrah
// Purpose : Default IPasswordService. The new password travels with the confirmation code, so
// the server never stores it between steps. The code is checked only after the password rules,
// and is consumed only when both pass.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Repositories;
using TeamSamsara.Shared.Alert;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Handlers;

public class PasswordService : IPasswordService
{
    #region Fields

    private readonly IUserStore _users;
    private readonly IAccountGateway _accounts;
    private readonly IVerificationCodeService _verification;
    private readonly IAlertSender _alerts;
    private readonly PasswordSettings _settings;
    private readonly ILogger<PasswordService> _logger;

    #endregion

    #region Constructors

    public PasswordService(
        IUserStore users,
        IAccountGateway accounts,
        IVerificationCodeService verification,
        IAlertSender alerts,
        IOptions<PasswordSettings> settings,
        ILogger<PasswordService> logger)
    {
        _users = users;
        _accounts = accounts;
        _verification = verification;
        _alerts = alerts;
        _settings = settings.Value;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    // Checks the password rules, then sends the confirmation code
    public async Task<PasswordCodeResult> RequestChangeCodeAsync(
        string userId,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var email = await FindMemberEmailAsync(userId);

        if (email is null)
        {
            return Rejected(PasswordStatus.AccountNotFound);
        }

        if (!MeetsRules(newPassword))
        {
            return Rejected(PasswordStatus.InvalidPassword);
        }

        var issue = await _verification.IssueAsync(
            userId, VerificationPurpose.StepUp, email, cancellationToken);

        var status = issue.Status == VerificationIssueStatus.Sent
            ? PasswordStatus.Success
            : PasswordStatus.CooldownActive;

        return new PasswordCodeResult(status, issue.CodeLength, issue.RetryAfterSeconds);
    }

    // Checks the password rules and the code, then replaces the password and ends every session
    public async Task<PasswordStatus> ChangeAsync(
        string userId,
        string newPassword,
        string code,
        CancellationToken cancellationToken = default)
    {
        var email = await FindMemberEmailAsync(userId);

        if (email is null)
        {
            return PasswordStatus.AccountNotFound;
        }

        // Before the code check, so an unacceptable password does not use the code up
        if (!MeetsRules(newPassword))
        {
            return PasswordStatus.InvalidPassword;
        }

        var verification = await _verification.VerifyAsync(
            userId, VerificationPurpose.StepUp, code, cancellationToken);

        if (verification != VerificationResult.Valid)
        {
            return ToFailureStatus(verification);
        }

        await _accounts.SetPasswordAsync(userId, newPassword);
        await _accounts.RevokeSessionsAsync(userId);
        await NotifyPasswordChangedAsync(email, cancellationToken);

        return PasswordStatus.Success;
    }

    #endregion

    #region Private Methods

    // The member's email, or null when there is no completed member account to act on
    private async Task<string?> FindMemberEmailAsync(string userId)
    {
        var user = await _users.GetByIdAsync(userId);

        if (user is null || user.AccessLevel != AccessLevel.Member)
        {
            return null;
        }

        return await _accounts.GetEmailAsync(userId);
    }

    // Whether the password satisfies the configured length rules
    private bool MeetsRules(string? password)
    {
        return password is not null
            && password.Length >= _settings.MinLength
            && password.Length <= _settings.MaxLength;
    }

    // Security notice after the change. The password is already replaced by now, so a failure
    // here is logged and must not turn a completed change into an error.
    private async Task NotifyPasswordChangedAsync(string email, CancellationToken cancellationToken)
    {
        try
        {
            await _alerts.SendAlertAsync(
                email,
                AlertType.PasswordChanged,
                new Dictionary<string, string>(),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Could not send the password-changed notice.");
        }
    }

    // Translates a failed code check into a password outcome
    private static PasswordStatus ToFailureStatus(VerificationResult verification)
    {
        return verification switch
        {
            VerificationResult.Invalid => PasswordStatus.InvalidCode,
            VerificationResult.Expired => PasswordStatus.CodeExpired,
            VerificationResult.TooManyAttempts => PasswordStatus.TooManyAttempts,
            VerificationResult.NotFound => PasswordStatus.NoPendingCode,
            _ => throw new ArgumentOutOfRangeException(
                nameof(verification), verification, "Not a failed verification result.")
        };
    }

    // A code request answer that carries no code details
    private static PasswordCodeResult Rejected(PasswordStatus status)
    {
        return new PasswordCodeResult(status, 0, 0);
    }

    #endregion
}
