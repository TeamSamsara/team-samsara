// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/PasswordResetDelivery.cs
// Version : 1.0.0
// Latest commit: feat/password-reset-service
// Author : Gerrah
// Purpose : Issues reset codes in the background and warns members of requests from unknown clients.

using Microsoft.Extensions.Logging;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Repositories;
using TeamSamsara.Shared.Alert;
using TeamSamsara.Shared.Context;

namespace TeamSamsara.Modules.Identity.Handlers;

public class PasswordResetDelivery : IPasswordResetDelivery
{
    #region Fields

    private readonly IUserStore _users;
    private readonly IAccountGateway _accounts;
    private readonly IVerificationCodeService _verification;
    private readonly IGuardDog _guardDog;
    private readonly IAlertSender _alerts;
    private readonly ILogger<PasswordResetDelivery> _logger;

    #endregion

    #region Constructors

    // Initializes the delivery with its stores, code service and notification channel.
    public PasswordResetDelivery(
        IUserStore users,
        IAccountGateway accounts,
        IVerificationCodeService verification,
        IGuardDog guardDog,
        IAlertSender alerts,
        ILogger<PasswordResetDelivery> logger)
    {
        _users = users;
        _accounts = accounts;
        _verification = verification;
        _guardDog = guardDog;
        _alerts = alerts;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    // Emails a reset code to a member, or stores a decoy code if the address has no member account.
    public async Task DeliverAsync(
        string email,
        ClientInfo client,
        CancellationToken cancellationToken = default)
    {
        var address = email.Trim();
        var member = await FindMemberAsync(address);

        if (member is null)
        {
            await IssueDecoyAsync(address, cancellationToken);
        }
        else
        {
            await IssueCodeAsync(member, address, client, cancellationToken);
        }
    }

    #endregion

    #region Private Methods

    // Members pending deletion are included; a reset does not restore the account.
    private async Task<User?> FindMemberAsync(string email)
    {
        var userId = await _accounts.GetUserIdByEmailAsync(email);

        if (userId is null)
        {
            return null;
        }

        var user = await _users.GetByIdAsync(userId);

        return user is { AccessLevel: AccessLevel.Member } ? user : null;
    }

    // Stores a code that is never sent, so unknown addresses behave like known ones.
    private async Task IssueDecoyAsync(string email, CancellationToken cancellationToken)
    {
        await _verification.IssueDecoyAsync(
            PasswordResetCrypto.HashEmail(email), VerificationPurpose.PasswordReset, cancellationToken);
    }

    // Emails the code, and warns the member if the request came from an unknown client.
    private async Task IssueCodeAsync(
        User member,
        string email,
        ClientInfo client,
        CancellationToken cancellationToken)
    {
        var issue = await _verification.IssueAsync(
            PasswordResetCrypto.HashEmail(email), VerificationPurpose.PasswordReset, email, cancellationToken);

        if (issue.Status != VerificationIssueStatus.Sent || _guardDog.IsKnown(member, client))
        {
            return;
        }

        await NotifyResetRequestedAsync(email, client, cancellationToken);
    }

    // Best effort: the code has already been sent.
    private async Task NotifyResetRequestedAsync(
        string email,
        ClientInfo client,
        CancellationToken cancellationToken)
    {
        var templateData = new Dictionary<string, string>
        {
            [AlertTemplateKeys.IpAddress] = client.IpAddress,
            [AlertTemplateKeys.UserAgent] = client.UserAgent
        };

        try
        {
            await _alerts.SendAlertAsync(
                email, AlertType.PasswordResetRequested, templateData, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Could not send the password-reset-requested notice.");
        }
    }

    #endregion
}
