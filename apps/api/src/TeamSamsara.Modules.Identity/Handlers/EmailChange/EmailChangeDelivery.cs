// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity/Handlers/EmailChange/EmailChangeDelivery.cs
// Version : 1.0.0
// Latest commit: feat/email-change-service
// Author : Gerrah
// Purpose : Issues the two email change codes in the background, using a decoy for an unavailable address.

using Microsoft.Extensions.Logging;
using TeamSamsara.Modules.Identity.Models;
using TeamSamsara.Modules.Identity.Repositories;

namespace TeamSamsara.Modules.Identity.Handlers;

public class EmailChangeDelivery : IEmailChangeDelivery
{
    #region Fields

    private readonly IAccountGateway _accounts;
    private readonly IVerificationCodeService _verification;
    private readonly ILogger<EmailChangeDelivery> _logger;

    #endregion

    #region Constructors

    // Initializes the delivery with the account gateway and code service.
    public EmailChangeDelivery(
        IAccountGateway accounts,
        IVerificationCodeService verification,
        ILogger<EmailChangeDelivery> logger)
    {
        _accounts = accounts;
        _verification = verification;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    // Emails a code to the current address, and one to the new address unless it is unavailable.
    public async Task DeliverAsync(
        string userId,
        string newEmail,
        CancellationToken cancellationToken = default)
    {
        var currentEmail = await _accounts.GetEmailAsync(userId);

        if (currentEmail is null)
        {
            return;
        }

        var available = await IsAvailableAsync(newEmail);

        await RunLoggedAsync(
            () => IssueAsync(userId, VerificationPurpose.EmailChangeOld, currentEmail, cancellationToken),
            "the current-address code");

        await RunLoggedAsync(
            () => available
                ? IssueAsync(userId, VerificationPurpose.EmailChangeNew, newEmail, cancellationToken)
                : IssueDecoyAsync(userId, cancellationToken),
            "the new-address code");
    }

    #endregion

    #region Private Methods

    // An address is unavailable if any account holds it, including the member's own.
    private async Task<bool> IsAvailableAsync(string email)
    {
        return await _accounts.GetUserIdByEmailAsync(email) is null;
    }

    // Generates a code and emails it.
    private async Task IssueAsync(
        string userId,
        VerificationPurpose purpose,
        string email,
        CancellationToken cancellationToken)
    {
        await _verification.IssueAsync(userId, purpose, email, cancellationToken);
    }

    // Stores a new-address code that is never sent.
    private async Task IssueDecoyAsync(string userId, CancellationToken cancellationToken)
    {
        await _verification.IssueDecoyAsync(userId, VerificationPurpose.EmailChangeNew, cancellationToken);
    }

    // A failed send must not stop the other code; the member can request again.
    private async Task RunLoggedAsync(Func<Task> work, string description)
    {
        try
        {
            await work();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Could not issue {Description} for an email change.", description);
        }
    }

    #endregion
}
